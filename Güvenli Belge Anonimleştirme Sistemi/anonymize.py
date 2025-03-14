# -*- coding: utf-8 -*-
import fitz  # PyMuPDF
import re
import spacy
import sys
import locale
import os

# 🛠 UTF-8 uyumluluğunu zorunlu kıl
os.environ["PYTHONUTF8"] = "1"  # Python'un iç UTF-8 desteğini aç
sys.stdout.reconfigure(encoding='utf-8')  # Terminal çıktısını UTF-8 yap
locale.setlocale(locale.LC_ALL, 'tr_TR.utf8')  # Yerel dil ayarını Türkçe yap

# SpaCy modelini yükle
try:
    nlp = spacy.load("en_core_web_trf")
except:
    print("'en_core_web_trf' yüklenemedi, 'en_core_web_sm' kullanılıyor...")
    nlp = spacy.load("en_core_web_sm")

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
        if ent.label_ in ["GPE", "LOC"]:  
            locations.add(ent.text)
        elif ent.label_ == "ORG":  
            organizations.add(ent.text)

    org_pattern = r"(?i)([A-Za-z\s]+University|Institute|College|Department|Lab|Center|Faculty)"
    extra_orgs = re.findall(org_pattern, text)
    organizations.update(extra_orgs)

    return list(locations), list(organizations)

def find_author_names(text):
    doc = nlp(text)
    possible_names = set()
    for ent in doc.ents:
        if ent.label_ == "PERSON":
            possible_names.add(ent.text)
    return list(possible_names)

def mask_pdf_all_pages(pdf_path, output_path, names, emails, locations, organizations):
    doc = fitz.open(pdf_path)

    for page in doc:
        for word_list, label in [(names, ""), (emails, ""), (locations, ""), (organizations, "")]:
            for w in word_list:
                rects = page.search_for(w)
                for rect in rects:
                    page.draw_rect(rect, color=(1, 1, 1), fill=(1, 1, 1))
                    page.insert_text((rect[0], rect[1]), label, fontsize=12, color=(0, 0, 0))

    doc.save(output_path)
    print(f" Anonimleştirilmiş PDF kaydedildi: {output_path}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Kullanım: python anonimize.py <input_pdf_path> <output_pdf_path>")
        sys.exit(1)

    input_pdf_path = sys.argv[1]
    output_pdf_path = sys.argv[2]

    text_between = extract_text_between_title_and_abstract(input_pdf_path)
