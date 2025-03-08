import spacy
import sys
import re
import json

# SpaCy modelini yükleyin
nlp = spacy.load("en_core_web_sm")

def extract_authors_and_emails(text):
    # Metni işleyin
    doc = nlp(text)

    # Yazar adlarını ve e-posta adreslerini saklamak için boş listeler oluşturun
    authors = []
    emails = []

    # Her bir varlığı kontrol edin
    for ent in doc.ents:
        if ent.label_ == "PERSON":
            authors.append(ent.text)

    # E-posta adreslerini bulmak için düzenli ifade
    email_pattern = r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}'
    emails = re.findall(email_pattern, text)

    # Sonuçları döndür
    return {"authors": authors, "emails": emails}

if __name__ == "__main__":
    # Komut satırından metni al
    input_text = sys.stdin.read()
    results = extract_authors_and_emails(input_text)

    # Sonuçları JSON formatında yazdır
    print(json.dumps(results))
