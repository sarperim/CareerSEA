# 🌊 CareerSEA 

![Status](https://img.shields.io/badge/status-development-orange)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![Python](https://img.shields.io/badge/python-3.11-blue)
![Aspire](https://img.shields.io/badge/Aspire-Orchestration-green)

**CareerSEA** is a cloud-native web application that uses advanced AI to provide personalized career predictions, helping graduates and professionals navigate the job market with confidence.

---

## The Problem
Career counseling is often expensive, static, or based on ineffective keyword matching. This leads to misalignment, low job satisfaction, and constant "job hopping."

##  The Solution
We bridge the gap by offering accessible, data-driven career guidance.
* **Personalized AI:** Uses **CareerBERT** (Rosenberger et al., 2025) and **Cosine Similarity** to match user resumes against a massive dataset of job descriptions.
* **Dynamic & Scalable:** A polyglot distributed system designed for parallel development and high availability.

---

## Tech Stack

We utilize a **Service-Based Architecture** orchestrated by **.NET Aspire**:

| Component | Tech | Role |
| :--- | :--- | :--- |
| **Frontend** | **Blazor Auto** | Hybrid rendering for a fast, interactive UI. |
| **Backend** | **ASP.NET Core** | REST API with Swagger UI. |
| **AI Engine** | **Python (FastAPI)** | Fine-tuned CareerBERT model for vector embeddings. |
| **Database** | **PostgreSQL** | Stores user profiles and career data. |
| **Orchestration** | **.NET Aspire** | Manages containers and service discovery. |

---

## Team & Responsibilities

CareerSEA is a collaborative effort combining Full Stack Engineering with advanced ML research.

| Team Member | Focus Area | Key Responsibilities |
| :--- | :--- | :--- |
| **Sarp** | **Full Stack & DevOps** | Blazor Frontend, ASP.NET Backend, .NET Aspire Orchestration, Docker Management, Database Design, and Cloud Deployment. |
| **Emir** | **AI Engineering** | Fine-tuning the CareerBERT model and implementing Cosine Similarity algorithms. |
| **Alphan** | **AI Engineering** | Fine-tuning the CareerBERT model and implementing Cosine Similarity algorithms. |

---

## Quick Start

This project uses **.NET Aspire** to spin up the frontend, backend, AI service, and database simultaneously.

1.  **Clone the repo:**
    ```bash
    git clone [https://github.com/yourusername/CareerSEA.git](https://github.com/yourusername/CareerSEA.git)
    ```
2.  **Install Python dependencies:**
    ```bash
    pip install -r src/CareerSEA.AI/requirements.txt
    ```
3.  **Run the AppHost:**
    ```bash
    dotnet run --project src/CareerSEA.AppHost
    ```

---

### 📚 References
* Rosenberger, J., et al. (2025). *CareerBERT: Matching Resumes to ESCO Jobs*.
