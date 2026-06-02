namespace Zogreo.Domain.Enums;

public enum Role { Applicant, Registrar, Bursar, SuperAdmin }

public enum ApplicationStatus
{
    Draft, Submitted, UnderReview, NeedsInfo, DocsVerified,
    OfferMade, OfferAccepted, FeesPaid, MedicalsCleared, Enrolled,
    Rejected, Withdrawn
}

public enum DocumentStatus { Pending, Verified, Rejected, NeedsResubmission }

public enum DocumentType
{
    NationalIdOrPassport, AcademicCertificate, PassportPhoto,
    MedicalReport, Other
}

public enum OfferStatus { Issued, Accepted, Declined, Expired }

public enum ProgramLevel { Certificate, Diploma, AdvancedDiploma, BibleCollege }

public enum DeliveryMode { Online, OnCampus, Blended }

public enum FeeCode { Application, Acceptance, Admission, Medicals, Technology, Tuition }

public enum InvoiceStatus { Unpaid, PartiallyPaid, Paid, Void }

public enum PaymentStatus { Pending, Success, Failed }

public enum PaymentChannel { Mpesa, Card, Other }

public enum NotificationChannel { Sms, Email }

public enum NotificationStatus { Queued, Sent, Failed }

public enum StudentStatus { Active, Deferred, Graduated, Withdrawn }
