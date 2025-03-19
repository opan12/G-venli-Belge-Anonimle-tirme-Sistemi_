import fitz  # PyMuPDF
import re
import spacy
import sys
from cryptography.fernet import Fernet

import os

# SpaCy modelini yükle
try:
    nlp = spacy.load("en_core_web_trf")  # Büyük model
except:
    print("'en_core_web_trf' modeli yüklenemedi, 'en_core_web_sm' modeli kullanılıyor...")
    nlp = spacy.load("en_core_web_sm")  # Alternatif küçük model

# Başlık ile Abstract arasındaki metni çıkarma
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
        if not found_title and re.search(r"\S", line):  # İlk dolu satırı başlık kabul et
            found_title = True
            continue  # Başlık satırını atla

        if re.search(r"(?i)\babstract\b", line):  # İlk "Abstract" kelimesini bul
            found_abstract = True
            break  # Abstract bulununca dur

        if found_title:
            extracted_lines.append(line)  # Başlıktan sonra gelen metinleri ekle

    return "\n".join(extracted_lines).strip() if found_abstract else ""

# E-posta adreslerini bul
def find_emails(text):
    email_pattern = r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
    return re.findall(email_pattern, text)

# Kurum ve konumları bul
def find_locations_and_orgs(text):
    doc = nlp(text)
    locations, organizations = set(), set()

    for ent in doc.ents:
        if ent.label_ in ["GPE", "LOC"]:  # Şehir, ülke, coğrafi yer
            locations.add(ent.text)
        elif ent.label_ == "ORG":  # Kurum, şirket, departman
            organizations.add(ent.text)

    org_pattern = r"(?i)([A-Za-z\s]+University|Institute|College|Department|Lab|Center|Faculty)"
    extra_orgs = re.findall(org_pattern, text)
    organizations.update(extra_orgs)  # Kurumları set'e ekleyelim

    return list(locations), list(organizations)

# Yazar isimlerini bul
def find_author_names(text):
    doc = nlp(text)
    possible_names = set()
    for ent in doc.ents:
        if ent.label_ == "PERSON":  # Kişi isimlerini al
            possible_names.add(ent.text)
    return list(possible_names)
# 🔑 Anahtarı oluşturup dosyaya kaydetme fonksiyonu
def generate_and_store_key():
    if not os.path.exists("secret.key"):
        key = Fernet.generate_key()
        with open("secret.key", "wb") as key_file:
            key_file.write(key)
        print(" Yeni şifreleme anahtarı oluşturuldu ve 'secret.key' dosyasına kaydedildi.")
    else:
        print(" 'secret.key' zaten var, yeni anahtar oluşturulmadı.")

# 🔑 Anahtarı dosyadan yükleme fonksiyonu
def load_key():
    with open("secret.key", "rb") as key_file:
        return key_file.read()

# Anahtarı oluştur ve yükle
generate_and_store_key()
key = load_key()
cipher = Fernet(key)

def encrypt_text(text):
    """Metni şifreler ve geri döndürür."""
    return cipher.encrypt(text.encode()).decode()

def mask_pdf_with_encryption(input_pdf_path, output_pdf_path, names, emails, locations, organizations, anonymization_options):
    """PDF içindeki bilgileri şifreleyerek anonimleştirir."""
    doc = fitz.open(input_pdf_path)

    for page in doc:
        if "names" in anonymization_options:
            for name in names:
                rects = page.search_for(name)
                encrypted_name = encrypt_text(name)[:10]  # Kısa versiyonunu al
                for rect in rects:
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_name, fontsize=12, color=(0, 0, 0))
        
        if "emails" in anonymization_options:
            for email in emails:
                rects = page.search_for(email)
                encrypted_email = encrypt_text(email)[:10]
                for rect in rects:
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_email, fontsize=12, color=(0, 0, 0))

        if "locations" in anonymization_options:
            for location in locations:
                rects = page.search_for(location)
                encrypted_location = encrypt_text(location)[:10]
                for rect in rects:
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_location, fontsize=12, color=(0, 0, 0))

        if "organizations" in anonymization_options:
            for organization in organizations:
                rects = page.search_for(organization)
                encrypted_organization = encrypt_text(organization)[:10]
                for rect in rects:
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), encrypted_organization, fontsize=12, color=(0, 0, 0))

    doc.save(output_pdf_path)
    print(f"PDF başarıyla kaydedildi: {output_pdf_path}")
    print(f"Şifreleme Anahtarı (Bunu sakla!): {key.decode()}")

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

    # PDF anonimleştirme işlemini başlat
    mask_pdf_with_encryption(input_pdf_path, output_pdf_path, names, emails, locations, organizations, anonymization_options)