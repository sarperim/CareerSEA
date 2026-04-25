import json
import os
from pathlib import Path
import re
import tempfile
from typing import List, Optional

import numpy as np
import ollama
import pymupdf4llm
import torch
import fitz
from fastapi import FastAPI, File, HTTPException, UploadFile
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer
import uvicorn

APP_TITLE = "CareerSEA BGE ESCO API"
APP_VERSION = "1.0.0"

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_LOCAL_ESCO_TITLES_PATH = REPO_ROOT / "CareerSEA.Web" / "CareerSEA.Web" / "JobTitles.cs"
DEFAULT_CONTAINER_ESCO_TITLES_PATH = Path("/app/data/esco_titles.json")

MODEL_DIR = Path(os.getenv("MODEL_DIR", "/app/model"))
ESCO_TITLES_PATH = os.getenv(
    "ESCO_TITLES_PATH",
    str(
        DEFAULT_LOCAL_ESCO_TITLES_PATH
        if DEFAULT_LOCAL_ESCO_TITLES_PATH.exists()
        else DEFAULT_CONTAINER_ESCO_TITLES_PATH
    ),
)
DEVICE = os.getenv("DEVICE", "cuda" if torch.cuda.is_available() else "cpu")
BGE_QUERY_PREFIX = os.getenv(
    "BGE_QUERY_PREFIX",
    "Represent this sentence for searching relevant passages: ",
)
NORMALIZE_EMBEDDINGS = os.getenv("NORMALIZE_EMBEDDINGS", "true").lower() == "true"
MAX_TOP_K = int(os.getenv("MAX_TOP_K", "50"))

app = FastAPI(title=APP_TITLE, version=APP_VERSION)


class EmbedRequest(BaseModel):
    texts: List[str] = Field(..., min_items=1)
    add_bge_prefix: bool = True


class PredictRequest(BaseModel):
    text: str = Field(..., min_length=1)
    top_k: int = Field(5, ge=1, le=50)
    add_bge_prefix: bool = True


class BatchPredictRequest(BaseModel):
    texts: List[str] = Field(..., min_items=1)
    top_k: int = Field(5, ge=1, le=50)
    add_bge_prefix: bool = True


class LegacyPredictJob(BaseModel):
    title: str = ""
    description: str = ""
    skills: str = ""


class LegacyPredictRequest(BaseModel):
    jobs: List[LegacyPredictJob] = Field(..., min_items=1)


class SearchResult(BaseModel):
    label: str
    score: float
    rank: int


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    model_dir: str
    device: str
    esco_titles_loaded: int
    embedding_dim: Optional[int] = None


class ModelService:
    def __init__(self) -> None:
        self.model: Optional[SentenceTransformer] = None
        self.esco_titles: List[str] = []
        self.esco_embeddings: Optional[np.ndarray] = None
        self.embedding_dim: Optional[int] = None

    def load(self) -> None:
        if not MODEL_DIR.exists():
            raise RuntimeError(f"MODEL_DIR does not exist: {MODEL_DIR}")

        self.model = SentenceTransformer(str(MODEL_DIR), device=DEVICE)
        self.embedding_dim = self.model.get_sentence_embedding_dimension()

        titles_path = Path(ESCO_TITLES_PATH)
        if titles_path.exists():
            self.esco_titles = self._load_esco_titles(titles_path)

            if self.esco_titles:
                self.esco_embeddings = self._encode(self.esco_titles, add_bge_prefix=False)

    def _load_esco_titles(self, titles_path: Path) -> List[str]:
        if titles_path.suffix.lower() == ".cs":
            return self._load_esco_titles_from_csharp(titles_path)
        return self._load_esco_titles_from_json(titles_path)

    def _load_esco_titles_from_json(self, titles_path: Path) -> List[str]:
        with open(titles_path, "r", encoding="utf-8") as f:
            payload = json.load(f)

        if isinstance(payload, dict):
            if "titles" in payload and isinstance(payload["titles"], list):
                return [str(x) for x in payload["titles"]]
            raise RuntimeError(
                "ESCO_TITLES_PATH JSON dict must contain a 'titles' list."
            )

        if isinstance(payload, list):
            return [str(x) for x in payload]

        raise RuntimeError("ESCO_TITLES_PATH must be a JSON list or dict with 'titles'.")

    def _load_esco_titles_from_csharp(self, titles_path: Path) -> List[str]:
        source = titles_path.read_text(encoding="utf-8")
        titles = re.findall(r'"((?:[^"\\]|\\.)*)"', source)
        if not titles:
            raise RuntimeError("No string titles found in JobTitles.cs.")
        return titles

    def _prepare_texts(self, texts: List[str], add_bge_prefix: bool) -> List[str]:
        prepared = []
        for text in texts:
            cleaned = text.strip()
            if not cleaned:
                prepared.append(cleaned)
                continue
            prepared.append(f"{BGE_QUERY_PREFIX}{cleaned}" if add_bge_prefix else cleaned)
        return prepared

    def _encode(self, texts: List[str], add_bge_prefix: bool = True) -> np.ndarray:
        if self.model is None:
            raise RuntimeError("Model is not loaded.")
        prepared = self._prepare_texts(texts, add_bge_prefix)
        embeddings = self.model.encode(
            prepared,
            convert_to_numpy=True,
            normalize_embeddings=NORMALIZE_EMBEDDINGS,
            show_progress_bar=False,
        )
        return embeddings.astype(np.float32)

    def embed(self, texts: List[str], add_bge_prefix: bool = True) -> List[List[float]]:
        return self._encode(texts, add_bge_prefix=add_bge_prefix).tolist()

    def predict_one(self, text: str, top_k: int = 5, add_bge_prefix: bool = True) -> List[SearchResult]:
        if self.esco_embeddings is None or not self.esco_titles:
            raise RuntimeError(
                "ESCO titles are not loaded. Provide ESCO_TITLES_PATH to enable /predict."
            )

        query_embedding = self._encode([text], add_bge_prefix=add_bge_prefix)[0]
        scores = np.matmul(self.esco_embeddings, query_embedding)
        k = min(top_k, len(self.esco_titles), MAX_TOP_K)
        top_indices = np.argpartition(-scores, kth=k - 1)[:k]
        top_indices = top_indices[np.argsort(-scores[top_indices])]

        return [
            SearchResult(label=self.esco_titles[idx], score=float(scores[idx]), rank=rank + 1)
            for rank, idx in enumerate(top_indices)
        ]


service = ModelService()


@app.on_event("startup")
def startup_event() -> None:
    service.load()


def build_legacy_prediction_text(jobs: List[LegacyPredictJob]) -> str:
    parts: List[str] = []
    for job in jobs:
        job_parts = []
        if job.title.strip():
            job_parts.append(f"Title: {job.title.strip()}")
        if job.description.strip():
            job_parts.append(f"Description: {job.description.strip()}")
        if job.skills.strip():
            job_parts.append(f"Skills: {job.skills.strip()}")
        if job_parts:
            parts.append(". ".join(job_parts))
    return "\n\n".join(parts).strip()


def extract_pdf_text(tmp_path: str) -> str:
    markdown_error: Optional[Exception] = None

    try:
        markdown = pymupdf4llm.to_markdown(tmp_path)
        if markdown and markdown.strip():
            return markdown
    except Exception as exc:
        markdown_error = exc

    try:
        with fitz.open(tmp_path) as pdf:
            text = "\n\n".join(page.get_text("text") for page in pdf)
    except Exception as exc:
        if markdown_error is not None:
            raise HTTPException(
                status_code=422,
                detail=f"PDF parsing failed: {markdown_error}; fallback text extraction failed: {exc}",
            ) from exc
        raise HTTPException(status_code=422, detail=f"PDF parsing failed: {exc}") from exc

    if text and text.strip():
        return text

    if markdown_error is not None:
        raise HTTPException(
            status_code=422,
            detail=f"PDF parsing failed: {markdown_error}. No extractable text found in the file.",
        )

    raise HTTPException(status_code=422, detail="No extractable text in PDF (possibly scanned or image-only).")


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        model_loaded=service.model is not None,
        model_dir=str(MODEL_DIR),
        device=DEVICE,
        esco_titles_loaded=len(service.esco_titles),
        embedding_dim=service.embedding_dim,
    )


@app.get("/")
def root() -> dict:
    return {
        "message": APP_TITLE,
        "version": APP_VERSION,
        "endpoints": ["/health", "/embed", "/predict", "/batch-predict"],
    }


@app.post("/embed")
def embed(request: EmbedRequest) -> dict:
    try:
        embeddings = service.embed(request.texts, add_bge_prefix=request.add_bge_prefix)
        return {
            "count": len(embeddings),
            "embedding_dim": service.embedding_dim,
            "embeddings": embeddings,
        }
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@app.post("/predict")
def predict(request: dict) -> dict:
    try:
        if "jobs" in request:
            legacy_request = LegacyPredictRequest(**request)
            prediction_text = build_legacy_prediction_text(legacy_request.jobs)
            if not prediction_text:
                raise HTTPException(status_code=422, detail="Prediction input was empty after formatting the submitted jobs.")

            predictions = service.predict_one(prediction_text)
            return {
                "best_job": predictions[0].label,
                "match_score": predictions[0].score,
                "recommendations": [
                    {"label": item.label, "score": item.score}
                    for item in predictions
                ],
            }

        parsed_request = PredictRequest(**request)
        predictions = service.predict_one(
            parsed_request.text,
            top_k=parsed_request.top_k,
            add_bge_prefix=parsed_request.add_bge_prefix,
        )
        return {
            "input": parsed_request.text,
            "top_k": parsed_request.top_k,
            "predictions": [item.model_dump() for item in predictions],
        }
    except HTTPException:
        raise
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


@app.post("/batch-predict")
def batch_predict(request: BatchPredictRequest) -> dict:
    try:
        outputs = []
        for text in request.texts:
            predictions = service.predict_one(
                text,
                top_k=request.top_k,
                add_bge_prefix=request.add_bge_prefix,
            )
            outputs.append(
                {
                    "input": text,
                    "predictions": [item.model_dump() for item in predictions],
                }
            )
        return {"count": len(outputs), "results": outputs}
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc


CV_MODEL = "qwen2.5:3b"

CV_SCHEMA = {
    "type": "object",
    "properties": {
        "experiences": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "title":       {"type": "string"},
                    "description": {"type": "string"},
                    "skills": {
                        "type": "array",
                        "items": {"type": "string"},
                    },
                },
                "required": ["title", "description", "skills"],
                "additionalProperties": False,
            },
        },
    },
    "required": ["experiences"],
    "additionalProperties": False,
}

CV_SYSTEM_PROMPT = """You extract work experience from a CV as structured data.

Output ONE object per distinct work experience (job, internship, freelance contract, or significant volunteer role). Order them from most recent to oldest.

For each experience:
- title: the job title exactly as it appears for that role (e.g., "Senior Backend Engineer"). If the title is missing, use the most specific role descriptor available.
- description: a 1-2 sentence factual summary of what the candidate did in THAT specific role. Do not summarize their whole career here. Do not invent details not present in the CV.
- skills: technical skills, tools, frameworks, and programming languages used in THAT specific role. Exclude soft skills. Use canonical names (e.g., "JavaScript" not "JS", "PostgreSQL" not "Postgres"). Deduplicate within the role.

If the CV lists no work experience, return an empty experiences array. Never merge multiple roles into one object. Never split one role into multiple objects."""


class ExtractedExperience(BaseModel):
    title: str
    description: str
    skills: list[str]


class ExtractedCv(BaseModel):
    experiences: list[ExtractedExperience]


_ollama_client = ollama.Client(host=os.environ.get("OLLAMA_HOST", "http://localhost:11434"))


@app.post("/extract-cv", response_model=ExtractedCv)
async def extract_cv(file: UploadFile = File(...)):
    content_type = (file.content_type or "").lower()
    filename = (file.filename or "").lower()
    if "pdf" not in content_type and not filename.endswith(".pdf"):
        raise HTTPException(status_code=415, detail="Only PDF uploads are supported.")

    data = await file.read()
    if not data:
        raise HTTPException(status_code=400, detail="Uploaded file is empty.")

    tmp_path = None
    try:
        with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as tmp:
            tmp.write(data)
            tmp_path = tmp.name
        markdown = extract_pdf_text(tmp_path)
    finally:
        if tmp_path and os.path.exists(tmp_path):
            os.unlink(tmp_path)

    try:
        response = _ollama_client.chat(
            model=CV_MODEL,
            format=CV_SCHEMA,
            messages=[
                {"role": "system", "content": CV_SYSTEM_PROMPT},
                {"role": "user", "content": markdown},
            ],
            options={"temperature": 0.1, "num_ctx": 8192},
        )
    except Exception as exc:
        raise HTTPException(status_code=503, detail=f"LLM service unavailable: {exc}") from exc

    raw = response["message"]["content"]
    try:
        parsed = json.loads(raw)
    except json.JSONDecodeError as exc:
        raise HTTPException(status_code=502, detail=f"LLM returned non-JSON: {exc}") from exc

    return ExtractedCv(**parsed)

if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8001, reload=True)
