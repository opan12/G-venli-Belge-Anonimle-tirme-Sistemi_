import fitz  # PyMuPDF
import re
import spacy
import sys
import locale
import os
from cryptography.fernet import Fernet  # Şifreleme için kullanılıyor

# UTF-8 uyumluluğunu zorunlu kıl
os.environ["PYTHONUTF8"] = "1"
sys.stdout.reconfigure(encoding='utf-8')
locale.setlocale(locale.LC_ALL, 'tr_TR.utf8')

# SpaCy modelini yükle
try:
    nlp = spacy.load("en_core_web_trf")
except:
    print("'en_core_web_trf' yüklenemedi, 'en_core_web_sm' kullanılıyor...")
    nlp = spacy.load("en_core_web_sm")

# 🎯 Hariç tutulacak yaygın bilimsel terimler / kısaltmalar
IGNORED_TERMS = {
    # EEG, MRI, CNN gibi tıbbi ve yapay zekâ terimleri
    "EEG", "ECG", "EKG", "fMRI", "PET", "CT", "CAT", "MRI", "EMG", "MEG", "NIRS", "TMS",
    "CNN", "LSTM", "RNN", "GAN", "SVM", "PCA", "t-SNE", "STFT", "DWT",
    "DEAP", "SEED", "PhysioNet", "HAM10000", "MNIST", "ImageNet", "CIFAR-10", "CIFAR-100",
    "FFT", "RMSE", "MSE", "AUC", "ROC", "TPR", "FPR"
}

def generate_key():
    return Fernet.generate_key()

def encrypt_data(data, key):
    fernet = Fernet(key)
    return fernet.encrypt(data.encode())

def decrypt_data(encrypted_data, key):
    fernet = Fernet(key)
    return fernet.decrypt(encrypted_data).decode()

def extract_text_between_title_and_abstract(pdf_path):
    doc = fitz.open(pdf_path)
    text = ""

    for page in doc:
        text += page.get_text("text") + "\n"

    lines = text.split("\n")
    extracted_lines = []
    found_abstract = False
    found_title = False

    for line in lines:
        if not found_title and re.search(r"\S", line):
            found_title = True
            continue
        if re.search(r"(?i)\babstract\b", line):
            found_abstract = True
            break
        if found_title:
            extracted_lines.append(line)

    return "\n".join(extracted_lines).strip() if found_abstract else ""

def find_emails(text):
    email_pattern = r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
    return re.findall(email_pattern, text)

def find_locations_and_orgs(text):
    doc = nlp(text)
    locations, organizations = set(), set()

    for ent in doc.ents:
        if ent.label_ in ["GPE", "LOC"] and ent.text not in IGNORED_TERMS:
            locations.add(ent.text)
        elif ent.label_ == "ORG" and ent.text not in IGNORED_TERMS:
            organizations.add(ent.text)

    org_pattern = r"(?i)([A-Za-z\s]+University|Institute|College|Department|Lab|Center|Faculty)"
    extra_orgs = re.findall(org_pattern, text)
    organizations.update({org for org in extra_orgs if org not in IGNORED_TERMS})

    return list(locations), list(organizations)

def find_author_names(text):
    doc = nlp(text)
    possible_names = set()
    for ent in doc.ents:
        if ent.label_ == "PERSON" and ent.text not in IGNORED_TERMS:
            possible_names.add(ent.text)
    return list(possible_names)

def mask_pdf_all_pages(input_pdf_path, output_pdf_path, names, emails, locations, organizations, anonymization_options, key):
    doc = fitz.open(input_pdf_path)

    for page in doc:
        if "names" in anonymization_options:
            for name in names:
                encrypted_name = encrypt_data(name, key)
                for rect in page.search_for(name):
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_name.decode(), fontsize=12, color=(0, 0, 0))

        if "emails" in anonymization_options:
            for email in emails:
                encrypted_email = encrypt_data(email, key)
                for rect in page.search_for(email):
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_email.decode(), fontsize=12, color=(0, 0, 0))

        if "locations" in anonymization_options:
            for location in locations:
                encrypted_location = encrypt_data(location, key)
                for rect in page.search_for(location):
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_location.decode(), fontsize=12, color=(0, 0, 0))

        if "organizations" in anonymization_options:
            for org in organizations:
                encrypted_org = encrypt_data(org, key)
                for rect in page.search_for(org):
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_org.decode(), fontsize=12, color=(0, 0, 0))

    try:
        doc.save(output_pdf_path)
        print(f"PDF başarıyla kaydedildi: {output_pdf_path}")
    except Exception as e:
        print(f"PDF kaydedilirken hata oluştu: {e}")

if __name__ == "__main__":
    if len(sys.argv) != 4:
        print("Kullanım: python anonimize.py <input_pdf_path> <output_pdf_path> <AnonymizationOptions>")
        sys.exit(1)

    input_pdf_path = sys.argv[1]
    output_pdf_path = sys.argv[2]
    anonymization_options = sys.argv[3].split(",")

    text_between = extract_text_between_title_and_abstract(input_pdf_path)
    emails = find_emails(text_between)
    names = find_author_names(text_between)
    locations, organizations = find_locations_and_orgs(text_between)

    key = generate_key()

    mask_pdf_all_pages(input_pdf_path, output_pdf_path, names, emails, locations, organizations, anonymization_options, key)
