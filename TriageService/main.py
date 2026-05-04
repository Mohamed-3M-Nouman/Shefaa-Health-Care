import os
import json
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import google.generativeai as genai

app = FastAPI(title="Shefaa AI Triage Nurse")

# Configure CORS for all origins
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configure Gemini
GEMINI_API_KEY = os.environ.get("GEMINI_API_KEY", "AIzaSyDeMaj9UuySK2r1x-XFxDoGKKZxf78IsOA")
genai.configure(api_key=GEMINI_API_KEY)

# Use gemini-1.5-flash for fast text generation
model = genai.GenerativeModel('gemini-1.5-flash')

class TriageRequest(BaseModel):
    symptoms: str

@app.post("/api/triage")
async def triage(request: TriageRequest):
    if not request.symptoms.strip():
        raise HTTPException(status_code=400, detail="Symptoms cannot be empty.")

    prompt = f"""
    You are an expert AI Triage Nurse for the 'Shefaa' HealthTech application.
    The patient has the following symptoms:
    "{request.symptoms}"

    Based on these symptoms, identify the most appropriate medical specialty (in Arabic) and provide a short, friendly piece of medical advice (in Arabic).

    You MUST strictly return a valid JSON object. Do not include markdown formatting, backticks, or any conversational text.
    The JSON object must contain exactly these two keys: "specialty" and "advice".
    
    Example format:
    {{
        "specialty": "باطنة",
        "advice": "يرجى شرب كميات كافية من السوائل والراحة."
    }}
    """

    try:
        response = model.generate_content(prompt)
        response_text = response.text.strip()
        
        # Clean up markdown code blocks if the model includes them despite instructions
        if response_text.startswith("```json"):
            response_text = response_text[7:]
        elif response_text.startswith("```"):
            response_text = response_text[3:]
            
        if response_text.endswith("```"):
            response_text = response_text[:-3]
            
        result_json = json.loads(response_text.strip())
        
        if "specialty" not in result_json or "advice" not in result_json:
            raise ValueError("Missing required keys in Gemini JSON response.")
            
        return result_json

    except json.JSONDecodeError:
        raise HTTPException(status_code=500, detail="Failed to parse AI response as JSON.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="[IP_ADDRESS]", port=8000, reload=True)
