# 🌊 CareerSEA 

**AI-Powered Personalized Career Path Recommendation Platform**

**CareerSEA** is a cloud-native and AI-powered web application that provides personalized career path predictions based on their previous job titles, descriptions, and skills. The system leverages a fine-tuned transformer-based ML-model (BERT) and semantic job embeddings to provide data-driven, personalized career guidance to help new graduates, students and professionals navigate the job market with confidence.

---

## The Problem
Career counseling is often expensive, static, based on ineffective keyword matching, and non-personalized. This leads to misalignment, low job satisfaction, and constant "job hopping."

## The Solution
CareerSEA addresses this gap by:
* Offering personalized, data-driven, and AI-based career guidance.
* Implementing a similar approach as **CareerBERT** (Rosenberger et al., 2025) suggests and **Cosine Similarity** method is used to match user resumes embeddings against a massive dataset of job embeddings.
* A polyglot distributed system designed for parallel development and high availability.
* Leveraging large-scale ESCO-mapped datasets.
* Remaining free and globally accessible.

---

## Tech Stack

We utilize a **Service-Based Architecture** orchestrated by **.NET Aspire**:

| Component | Tech | Role |
| :--- | :--- | :--- |
| **Frontend** | **Blazor Auto** | Hybrid rendering for a fast, interactive UI. |
| **Backend** | **ASP.NET Core** | REST API with Swagger UI. |
| **AI Model** | **Python (FastAPI)** | Fine-tuned BERT (bert-large-uncased-whole-word-masking) model for semantic job and user embedding extraction. Cosine similarity for semantic matching and recommendation system. |
| **Database** | **PostgreSQL** | Stores user profiles and career data. |
| **Orchestration** | **.NET Aspire** | Manages containers and service discovery. |

### Dataset Details
1. Source: KARRIEREWEGE (German Employment Agency)
2. Features used:
	1. preferredLabel_en (Job title)
	2. description_en (Job description)
	3. skills (ESCO skills)
3. Label Processing:
	1. String labels → numeric IDs
	2. Low-frequency labels filtered
	3. Balanced sampling

---

## Team & Responsibilities

CareerSEA is a collaborative effort combining Full Stack Engineering with advanced ML research.

| Team Member | Focus Area | Key Responsibilities |
| :--- | :--- | :--- |
| **Sarp** | **Full Stack & DevOps** | Blazor Frontend, ASP.NET Backend, .NET Aspire Orchestration, Docker Management, Database Design, and Cloud Deployment. |
| **Emir** | **AI Engineering** | Dataset preprocessing, BERT model fine-tuning, embeddings extraction and cosine similarity matching. |
| **Alphan** | **AI Engineering** | AI model integration with FastAPI, recommendations generation. |

---

## Quick Start

This project uses **.NET Aspire** to spin up the frontend, backend, AI service, and database simultaneously.

1.  **Clone the repo:**
    ```bash
    git clone [https://github.com/sarperim/CareerSEA.git
    ```
2.  **Install Python dependencies:**
    ```bash
    pip install -r src/CareerSEA.Py/requirements.txt
    ```
3.  **Run the AppHost:**
    ```bash
    cd CareerSEA.AppHost && dotnet run
    ```

---

## References
* Rosenberger, J., Wolfrum, L., Weinzierl, S., Kraus, M., & Zschech, P. (2025). CareerBERT: Matching Resumes to ESCO Jobs in a Shared Embedding Space for Generic Job Recommendations. ArXiv.org. https://arxiv.org/abs/2503.02056
* Senger, E., Campbell, Y., van, & Plank, B. (2024). KARRIEREWEGE: A Large Scale Career Path Prediction Dataset. ArXiv.org. https://arxiv.org/abs/2412.14612
‌
