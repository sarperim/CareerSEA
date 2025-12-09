#Integreted to C# Web-App by Sarp
#Bert model trained and structured by Emir
#Numpy cosine matching and fast api by Alphan

#Importing the necessary modules, frameworks and libraries
#Imports FastAPI framework for API implementation
from fastapi import FastAPI
#For defining the body structure for /POST requests
from pydantic import BaseModel
# to handle numeric arrays (our embeddings and labels .npy)
import numpy as np
# to run the BERT model and work with tensors
import torch
#for loading the tokenizer and the BERT model
from transformers import BertTokenizerFast, AutoModelForSequenceClassification
#To compute the cosine similarity between the embeddings
from sklearn.metrics.pairwise import cosine_similarity
#to load the job labels mapping json file
import json
import uvicorn



#initializing the API
app = FastAPI(title="CareerSEA API", version="2.0")

#Paths for the Fine-Tuned BERT model and the tokenizer
MODEL_PATH = "./fine_tuned_BERT"
TOKENIZER_PATH = "./tokenizer"

#loading the tokenizer
tokenizer = BertTokenizerFast.from_pretrained(TOKENIZER_PATH)

#loading the AI model (requesting hidden states as they are required for embeddings)
model = AutoModelForSequenceClassification.from_pretrained(MODEL_PATH, output_hidden_states=True)

#Tells the model that we are in evaluation mode:
#This disables the Dropout layers which is the process of turning off some neurons to reduce overfitting
#during training. We disable it to make our prediction stable and deterministic
model.eval()

#loading the job embeddings
embeddings = np.load("./job_embeddings.npy")
#loading the job labels
labels = np.load("./job_labels.npy")

# loads job label mappings (label -> ID mapping)
with open("./job_label_mapping.json") as f:
    mapping = json.load(f)

#extracts the dictionary that maps numeric label IDs to readable job titles
id2label = mapping["id2label"]


#takes a string as input, converts it to a 1024-dimensional vector and returns the vector
def get_embedding(text):
    #tokenizes the text
    #max length is 80 tokens (if # of tokens<80 -> padding, if # of tokens > 80 -> truncation)
    #Output is pytorch tensors
    #Tensor: A multi dimensional array used in deep learning optimized for GPU
    #Tensor shapes (Scalar: [], Vector: [3], Matrix: [3,3] , 3D: [3,3,3])
    #We use tensors since they are integrated with BERT(BERT hidden states are returned as tensors)
    # and also since they are optimized for GPU, they allow us to perform fast GPU operations
    inputs = tokenizer(
        text,
        return_tensors="pt",
        truncation=True,
        max_length=80,
        padding="max_length"
    )

    #Tells pytorch to not track gradients which makes evaluation faster(always use this when evaluating)
    #Gradients: How much each weight(parameter) needs to change to reduce the error.
    #During the training, the model:
    #1.Makes a prediction
    #2.Sees how wrong it is and use gradients to adjust weights and improve
    #If the gradient is 0(no need for improvement), big negative(decrease the weight),
    # big positive(increase the weight)
    with torch.no_grad():
        #Runs the input through the model by unpacking the input dictionary into arguments
        outputs = model(**inputs, output_hidden_states=True)

    #outputss.hidden states has 13 layers(0:token embeddings, 1-12: transformer block outputs)
    #We take the last layer (-1) since it is for task-adapted contextual meaning which is what we need
    #for generating job embeddings for similarity
    hidden = outputs.hidden_states[-1]

    #Each Tensor shape in BERT-Large: [1, 80, 1024] but the original attention mask is [1,80]
    #so we unsqueeze it with -1 to add a new dimension to it to make it compatible with the tensor shape
    mask = inputs["attention_mask"].unsqueeze(-1)

    #Masked mean pooling (better CLS token(beginning of each sentence) which is better for similarity tasks)
    #(hidden * mask): zeroes out all the paddings so that they won't affect the mean
    #(hidden * mask).sum(1): Sums of all real(no padding) tokens, mask.sum(1):number of real tokens
    mean_emb = (hidden * mask).sum(1) / mask.sum(1) #produces the mean vector for all real tokens

    #Removing the batch dimension: [1,1024] -> [1024] (gives us a simple ID embedding vector)
    mean_emb = mean_emb.squeeze(0)

    #L2 normalization (dividing the vector by its length, making its length 1)
    #We normalize so that all vectors have the same scale and similarity is only calculated based on
    #the direction(meaning), not the length
    mean_emb = mean_emb / mean_emb.norm()

    #convert the tensor into numpy array so that it could be stored and processed later on and
    #return it as the output
    return mean_emb.numpy()

#Generate embeddings for multiple jobs, in case the user enters multiple jobs instead of one
def embed_multiple_jobs(job_list):
    #defining an array of embeddings
    embs = []

    #BERT expects a single sentence so combine the details of every job into a single text
    for job in job_list:
        text = (
            f"Title: {job['title']}. "
            f"Description: {job['description']}. "
            f"Skills: {job['skills']}."
        )

        embs.append(get_embedding(text))#get the embeddings for the combined text

    #np.stack converts the embeddings list to a 2D array (num_jobs, embedding_dimensions)
    #np.mean(...,axis = 0) averages across all the jobs and produce a single embedding vector for all the jobs
    return np.mean(np.stack(embs), axis=0)


#function to predict the next job for the user
def predict_next_jobs(job_list):
    user_emb = embed_multiple_jobs(job_list) #get the job embeddings

    #user embeddings are 1D vectors of shape(1024,) but cosine similarity requires 2D matrices(consisting of 2 vectors)
    #so we convert the user embeddings into 2D vectors via user_emb.reshape(1, -1)
    #(1024,) -> (1,1024): 1 vector,with 1024 dimensions
    #After resizing the cosine similarity is calculated for all embeddings, comparing
    #the similarity between the user embeddings and the dataset embeddings
    #[0] at the end removes the batch dimension and returns a simple 1D array
    sims = cosine_similarity(user_emb.reshape(1, -1), embeddings)[0]

    #find the index with the highest similarity score
    best_idx = sims.argmax()
    #find the best job label based on the index found earlier
    best_label = id2label[str(labels[best_idx])]
    #find the best match score based on the index found earlier
    best_score = float(sims[best_idx])

    #defining the top 5 recommendation list and the seen variable to ignore duplicates
    unique_recommendations = []
    seen = set()

    # sorts the array in descending order[::-1] and returns the top 5 most similar jobs
    sorted_indices = sims.argsort()[::-1]
    # check all the rows to find the best job label, best match score, and top 5 jobs
    for idx in sorted_indices:
        job_label = id2label[str(labels[idx])]
        if job_label not in seen:
            unique_recommendations.append({
                "label": job_label,
                "score": float(sims[idx])
            })
            seen.add(job_label)

        if len(unique_recommendations) == 5:
            break
    #returning the predicted jobi match score, top 5 similar jobs and user embeddings
    return best_label, best_score, unique_recommendations, user_emb


#Each job entry consists of a job title, job description and the skills used in that job
class Job(BaseModel):
    title: str
    description: str
    skills: str

#User might input one or multiple jobs so i defined the user input as the list of jobs
class UserInput(BaseModel):
    jobs: list[Job]


# the prediction endpoint (the main logic of the program)
@app.post("/predict")
def predict_api(input_data: UserInput):
    #to convert the job list from pydantic(/POST request data) into regular python dictionary
    job_list = [job.dict() for job in input_data.jobs]

    #get the predictions from the prediction function
    best_job, best_score, recommendations, user_emb = predict_next_jobs(job_list)

    #Send the results to the C# app
    return {
        "best_job": best_job,
        "match_score": best_score,
        "recommendations": recommendations
    }

# the root (home) endpoint
@app.get("/")
def home():
    return {"message": "CareerSEA API is running!"}


#Testing with sample data to see if it works
if __name__ == "__main__":
   # This ensures the server starts when the script is run directly
   uvicorn.run(app, host="0.0.0.0", port=8001)
   """ 
    test_jobs =  [
        {
            "title": "Gymnast",
            "description": "Competes in high level gymanstics",
            "skills":"Iron cross, maltase, handstand"
        },
        {
            "title": "Lifting instructor",
            "description": "Makes people lift weights",
            "skills":"powerlifting, calisthenics"
        },
    ]

    best_job, best_score, recommendations, _ = predict_next_jobs(test_jobs)

    print("Best Job:", best_job)
    print("Match Score:", best_score)
    print("Top 5 Best Jobs:")
    for rec in recommendations:
        print(rec)
"""


