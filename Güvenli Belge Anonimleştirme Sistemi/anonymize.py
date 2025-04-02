import fitz  # PyMuPDF
import re
import spacy
import sys
import locale
import os
from cryptography.fernet import Fernet  # Şifreleme için kullanılıyor
import cv2
import numpy as np
from PIL import Image
import json

# UTF-8 uyumluluğunu zorunlu kıl
os.environ["PYTHONUTF8"] = "1"
sys.stdout.reconfigure(encoding='utf-8')
locale.setlocale(locale.LC_ALL, 'tr_TR.utf8')

# SpaCy modeli
try:
    nlp = spacy.load("en_core_web_trf")
except:
    print("'en_core_web_trf' yüklenemedi, 'en_core_web_sm' kullanılıyor...")
    nlp = spacy.load("en_core_web_sm")

IGNORED_TERMS = {
    "EEG", "ECG", "EKG", "fMRI", "PET", "CT", "CAT", "MRI", "EMG", "MEG", "NIRS", "TMS",
    "CNN", "LSTM", "RNN", "GAN", "SVM", "PCA", "t-SNE", "STFT", "DWT",
    "DEAP", "SEED", "PhysioNet", "HAM10000", "MNIST", "ImageNet", "CIFAR-10", "CIFAR-100",
    "FFT", "RMSE", "MSE", "AUC", "ROC", "TPR", "FPR"
}

# --- METİN ÇIKARMA ---
def extract_text_between_title_and_abstract(pdf_path):
    doc = fitz.open(pdf_path)
    text = ""
    for page in doc:
        text += page.get_text("text") + "\n"

    lines = text.split("\n")
    extracted_lines = []
    found_title = False
    found_abstract = False

    for line in lines:
        if not found_title and re.search(r"\S", line):
            found_title = True
            continue
        if re.search(r"(?i)\babstract\b", line):
            found_abstract = True
            break
        if found_title:
            extracted_lines.append(line)

    return "\n".join(extracted_lines).strip() if found_abstract else text.strip()

# --- E-POSTA & İSİM ---
def find_emails(text):
    pattern = r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
    return re.findall(pattern, text)

def extract_name_from_email(email):
    name_part = email.split('@')[0]
    name = re.sub(r'[^a-zA-Z.]', '', name_part)
    name_parts = name.split('.')

    if len(name_parts) > 1:
        first_name = name_parts[1].capitalize()
        last_name = name_parts[0].capitalize()
        return f"{last_name}. {first_name}"
    return name.capitalize()

def find_locations_and_orgs(text):
    doc = nlp(text)
    organizations ,locations = set(), set()

    for ent in doc.ents: 
        if ent.label_ == "ORG" and ent.text not in IGNORED_TERMS:
            organizations.add(ent.text.strip())  # strip() ile baştaki ve sondaki boşlukları kaldırıyoruz
        elif ent.label_ in ["GPE", "LOC"] and ent.text not in IGNORED_TERMS:
            locations.add(ent.text.strip())  # strip() ile baştaki ve sondaki boşlukları kaldırıyoruz
       

    # Daha iyi tanımlama için ek kontroller
    additional_patterns = [
        r"[A-Za-z\s]+ University of [A-Za-z\s]+",  # 'University of' ile başlayan organizasyonlar
        r"[A-Za-z\s]+ Department of [A-Za-z\s]+",  # 'Department of' ile başlayan organizasyonlar
        
    ]

    # "university of" ve "department of" içeren organizasyonları bulmak
    for pattern in additional_patterns:
        matches = re.findall(pattern, text)
        for match in matches:
            organizations.add(match.strip())  # strip() ile baştaki ve sondaki boşlukları kaldırıyoruz

    return list(organizations),list(locations)

# --- YAZAR İSİMLERİ ---
def find_author_names(text):
    doc = nlp(text)
    names = set()
    for ent in doc.ents:
        if ent.label_ == "PERSON" and ent.text not in IGNORED_TERMS:
            names.add(ent.text.strip())
    return list(names)

# --- EPOSTADAN BULUNAN İSİMLERİ YAZARA EKLE ---
def add_email_names_to_authors(emails, author_names, text):
    email_to_name = {}

    # İlk olarak, her bir e-posta adresini kontrol edip isimleri çıkaralım
    for email in emails:
        name = extract_name_from_email(email)  # Burada isim çıkartılıyor
        email_to_name[email] = name  # e-posta ile isim eşleşmesini kaydediyoruz
        
        # Eğer isim metinde varsa, yazara ekle
        if name not in author_names and name in text:
            author_names.append(name)


    return author_names, email_to_name

# --- YAZAR VE ORGANİZASYONLARI AYIR ---
def filter_authors_and_organizations(authors, organizations):
    filtered_orgs = [org for org in organizations if org not in authors]
    return authors, filtered_orgs

# --- ŞİFRELEME ---
def generate_key():
    return Fernet.generate_key()

def encrypt_data(data, key):
    fernet = Fernet(key)
    return fernet.encrypt(data.encode())

def decrypt_data(encrypted_data, key):
    fernet = Fernet(key)
    return fernet.decrypt(encrypted_data).decode()


def anonymize_pdf(input_pdf_path, output_pdf_path, names, emails, locations, organizations, key, options):
    try:
        doc = fitz.open(input_pdf_path)
        if not doc:
            print(f"Hata: PDF açılamadı: {input_pdf_path}")
            return
    except Exception as e:
        print(f"Hata: PDF açılamadı: {e}")
        return

    encrypted_data = {}
    anonymized_terms = set()  # Daha önce anonimleştirilen terimler için bir küme oluşturuyoruz
    term_positions = {}  # Aynı terim için konum kontrolü yapacağız

    # Tüm terimleri birleştiriyoruz
    all_terms = []
    term_types = []  # Tür bilgilerini tutacak liste
    if 'emails' in options:
        all_terms += emails
        term_types += ['email'] * len(emails)
    if 'names' in options:
        all_terms += names
        term_types += ['name'] * len(names)
   
    if 'organizations' in options:
        all_terms += organizations
        term_types += ['organization'] * len(organizations)
    if 'locations' in options:
        all_terms += locations
        term_types += ['location'] * len(locations)
    
        
    email_to_name = {}  # E-posta -> isim eşlemesi
    
    # Yazar isimleri ve e-posta eşlemesi
    author_names, email_to_name = add_email_names_to_authors(emails, names, input_pdf_path)

    for page_num, page in enumerate(doc):
        page_data = []

        for term, term_type in zip(all_terms, term_types):
      
            # Eğer terim '\n' içeriyorsa, terimi ayıralım
            if '\n' in term:
                sub_terms = term.split('\n')
            else:
                sub_terms = [term]

            # Her parçayı kontrol et
            for sub_term in sub_terms:
                term_enc = encrypt_data(sub_term, key).decode()
                matches = page.search_for(sub_term, quads=False)

                if not matches:
                    print(f"UYARI: '{sub_term}' PDF içinde bulunamadı!")
                    continue

                for rect in matches:
                    # Aynı terim için konum kontrolü yapıyoruz
                    position_key = (rect.x0, rect.y0)
                    if position_key in term_positions and term_positions[position_key] == sub_term:
                        continue  # Aynı konumda tekrar ekleme
                    term_positions[position_key] = sub_term

                    # Termi anonimleştir
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page_data.append({
                        "text": sub_term,
                        "encrypted_text": term_enc,
                        "type": term_type,  # Tür bilgisi eklendi
                        "page": page_num,
                        "rect": [rect.x0, rect.y0, rect.x1, rect.y1]
                    })

                # Bu terimi anonimleştirdiğimiz için kümesine ekliyoruz
                anonymized_terms.add(sub_term)

        if page_data:
            encrypted_data[page_num] = page_data

    # JSON dosyası kaydı
    with open("encrypted_data.json", "w", encoding="utf-8") as file:
        json.dump(encrypted_data, file, ensure_ascii=False, indent=4)

    try:
        doc.save(output_pdf_path)
        print(f"PDF başarıyla kaydedildi: {output_pdf_path}")
        print(f"Şifrelenmiş metinler 'encrypted_data.json' dosyasına yazıldı.")
    except Exception as e:
        print(f"Hata: PDF kaydedilemedi: {e}")


def blur_faces_in_pdf(output_pdf_path, blur_pdf_path):
    # PDF dosyasını aç
    doc = fitz.open(output_pdf_path)

    # Son sayfayı al
    last_page = doc[-1]
    pix = last_page.get_pixmap()

    # Pixmap'i PIL image formatına çevir
    img = Image.frombytes("RGB", [pix.width, pix.height], pix.samples)
    img_array = np.array(img)

    # Yüz tespiti için cascade sınıfını yükle
    face_cascade = cv2.CascadeClassifier(cv2.data.haarcascades + 'haarcascade_frontalface_default.xml')
    gray = cv2.cvtColor(img_array, cv2.COLOR_RGB2GRAY)

    # Yüzleri tespit et
    faces = face_cascade.detectMultiScale(gray, scaleFactor=1.1, minNeighbors=5, minSize=(30, 30))

    # Yüzleri bulanıklaştır
    for (x, y, w, h) in faces:
        img_array[y:y+h, x:x+w] = cv2.GaussianBlur(img_array[y:y+h, x:x+w], (99, 99), 30)

    # Bulanıklaştırılmış resmi kaydet
    blurred_img = Image.fromarray(img_array)
    temp_img_path = "temp_last_page.png"
    blurred_img.save(temp_img_path)

    # PDF'e resim ekle
    last_page.insert_image(last_page.rect, filename=temp_img_path)

    # Geçici dosyayı sil
    os.remove(temp_img_path)

    # PDF'i kaydet
    doc.save(blur_pdf_path)
    print(f"Bulanıklaştırılmış PDF başarıyla kaydedildi: {blur_pdf_path}")

# --- MAIN ---
if __name__ == "__main__":
    if len(sys.argv) != 5:
        print("Kullanım: python anonimleştir.py <input_pdf> <output_pdf> <blur_pdf> <opsiyonlar>")
        sys.exit(1)

    input_pdf_path = sys.argv[1]
    output_pdf_path = sys.argv[2]
    blur_pdf_path = sys.argv[3]
    anonymization_options = sys.argv[4].split(",")

    text = extract_text_between_title_and_abstract(input_pdf_path)
    emails = find_emails(text)
    author_names = find_author_names(text)
    author_names, email_to_name = add_email_names_to_authors(emails, author_names, text)
    organizations,locations = find_locations_and_orgs(text)
    author_names, organizations = filter_authors_and_organizations(author_names, organizations)
    

    key = generate_key()
    with open("encryption_key.key", "wb") as kf:
        kf.write(key)

    anonymize_pdf(input_pdf_path,output_pdf_path,author_names,emails,locations,organizations,key,anonymization_options)
    blur_faces_in_pdf(output_pdf_path,blur_pdf_path)