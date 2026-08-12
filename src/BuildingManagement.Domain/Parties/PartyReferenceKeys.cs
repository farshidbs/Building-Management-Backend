namespace BuildingManagement.Domain;

public static class PartyReferenceKeys
{
    public static class PartyTypes
    {
        public const string IranianPerson = "iranian_person";
        public const string IranianOrganization = "iranian_organization";
        public const string ForeignPerson = "foreign_person";
        public const string ForeignOrganization = "foreign_organization";
    }

    public static class ContactTypes
    {
        public const string Mobile = "mobile";
        public const string Phone = "phone";
        public const string Email = "email";
    }

    public static class RelationTypes
    {
        public const string Owner = "owner";
        public const string Tenant = "tenant";
        public const string Resident = "resident";
        public const string LegalRepresentative = "legal_representative";
        public const string ContactPerson = "contact_person";
        public const string Other = "other";
    }
}
