import sys
import json
import os
import fitz  # PyMuPDF
from cryptography.fernet import Fernet

def load_key():
    """Şifreleme anahtarını dosyadan yükler"""
    key_path = "encryption_key.key"
    if not os.path.exists(key_path):
        print("HATA: 'encryption_key.key' dosyası bulunamadı!")
        sys.exit(1)

    print("Şifreleme anahtarı yükleniyor...")
    with open(key_path, "rb") as key_file:
        return key_file.read()

def decrypt_data(encrypted_data, key):
    """Şifrelenmiş veriyi çözümler"""
    fernet = Fernet(key)
    return fernet.decrypt(encrypted_data.encode()).decode()

# Komut satırından PDF dosya yolunu al
if len(sys.argv) < 2:
    print("HATA: PDF dosya yolu belirtilmedi!")
    sys.exit(1)

input_pdf = sys.argv[1]  # Dosya yolunu al

# Dosyanın gerçekten var olup olmadığını kontrol et
if not os.path.exists(input_pdf):
    print(f"HATA: '{input_pdf}' PDF dosyası bulunamadı!")
    sys.exit(1)

print(f"İşlem başarılı: {input_pdf} dosyası alındı.")

def write_to_pdf(json_file, input_pdf, output_pdf):
    """JSON'daki verileri deşifre edip, belirtilen konumlara göre PDF'e yazar"""
    if not os.path.exists(json_file):
        print(f"HATA: '{json_file}' dosyası bulunamadı!")
        sys.exit(1)

    if not os.path.exists(input_pdf):
        print(f"HATA: '{input_pdf}' PDF dosyası bulunamadı!")
        sys.exit(1)

    print(f"JSON dosyası okunuyor: {json_file}")
    key = load_key()

    with open(json_file, "r", encoding="utf-8") as file:
        encrypted_json = json.load(file)

    print(f"PDF açılıyor: {input_pdf}")
    doc = fitz.open(input_pdf)

    # Sayfa başına yerleştirilen metin konumları
    page_text_positions = {page_num: [] for page_num in encrypted_json.keys()}

    for page_num, items in encrypted_json.items():
        print(f"Sayfa {page_num} işleniyor...")
        page = doc[int(page_num)]

        for item in items:
            decrypted_text = decrypt_data(item["encrypted_text"], key)
            x0, y0, _, _ = item["rect"]

            # Aynı konumda tekrar metin yazılmaması için kontrol ekleyelim
            if any([abs(x0 - x) < 5 and abs(y0 - y) < 5 for (x, y) in page_text_positions[page_num]]):
                print(f"UYARI: Aynı pozisyonda metin tekrar yazılmak üzere: {decrypted_text}")
                continue  # Aynı pozisyonda tekrar yazmayı atla

            # Konumu kaydediyoruz
            page_text_positions[page_num].append((x0, y0))

            print(f"Yazılacak metin: {decrypted_text} -> ({x0}, {y0}) konumuna eklenecek.")
            page.insert_text((x0, y0), decrypted_text, fontsize=10, color=(1, 0, 0))

    print(f"Yeni PDF kaydediliyor: {output_pdf}")
    doc.save(output_pdf)
    doc.close()
    print(f"PDF oluşturuldu: {output_pdf}")

    # İşletim sistemine göre PDF dosyasını aç
    if os.name == "nt":
        os.system(f"start {output_pdf}")  # Windows
    elif os.name == "posix":
        os.system(f"xdg-open {output_pdf}")  # Linux
    elif os.name == "darwin":
        os.system(f"open {output_pdf}")  # macOS


# Kullanım
json_file = "encrypted_data.json"
output_pdf = "decrypted_output.pdf"

write_to_pdf(json_file, input_pdf, output_pdf)