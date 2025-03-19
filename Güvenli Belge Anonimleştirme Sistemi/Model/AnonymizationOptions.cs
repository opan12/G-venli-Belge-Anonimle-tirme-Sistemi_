namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class AnonymizationOptions
    {
        public bool MaskAuthors { get; set; }
        public bool MaskOrganizations { get; set; }
        public bool MaskEmails { get; set; }
        public bool MaskPhoneNumbers { get; set; }
        public bool MaskLocations { get; set; }
    }

}
