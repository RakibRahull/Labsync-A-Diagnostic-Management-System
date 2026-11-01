using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabSync.Areas.LabTechnician.Models
{
    // =========================================================
    // TEST RESULT LIST ROW
    // =========================================================

    public class TestResultListRowViewModel
    {
        public int TestResultId { get; set; }

        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }


        // PATIENT

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? Phone { get; set; }


        // TEST

        public string? TestCode { get; set; }

        public string TestName { get; set; } = string.Empty;

        public string? Unit { get; set; }


        // REFERENCE

        public string? ReferenceRange { get; set; }

        public string? ReferenceRangeDescription { get; set; }


        // RESULT

        public string? ResultValue { get; set; }

        public string? ResultInterpretation { get; set; }

        public string? Remarks { get; set; }

        public int ResultStatusId { get; set; }

        public string ResultStatusName { get; set; } = string.Empty;

        public DateTime? ResultDate { get; set; }


        // SAMPLE

        public int SampleStatusId { get; set; }

        public string SampleStatusName { get; set; } = string.Empty;
    }


    // =========================================================
    // TEST RESULT ENTRY
    // =========================================================

    public class TestResultEntryViewModel
    {
        public int TestResultId { get; set; }


        // APPOINTMENT

        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }


        // PATIENT

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }


        // TEST

        public int TestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string? Unit { get; set; }

        public string? TurnAroundTime { get; set; }


        // REFERENCE RANGE

        public int ReferenceRangeId { get; set; }

        public string? ReferenceRange { get; set; }

        public string? ReferenceRangeDescription { get; set; }


        // SAMPLE

        public int SampleStatusId { get; set; }

        public string SampleStatusName { get; set; } = "Pending";

        public int? SampleTypeId { get; set; }

        public string? SampleTypeName { get; set; }


        // RESULT STATUS

        public int ResultStatusId { get; set; }

        public string? ResultStatusName { get; set; }


        // RESULT VALUE

        public string? ResultValue { get; set; }


        // NORMAL / ABNORMAL

        public string? ResultInterpretation { get; set; }


        // REMARKS

        public string? Remarks { get; set; }


        // DATE

        public DateTime? ResultDate { get; set; }


        // UI ONLY

        [NotMapped]
        public bool HasExistingResult { get; set; }


        [NotMapped]
        public List<ResultStatusOptionViewModel> ResultStatuses
        {
            get;
            set;
        } = new List<ResultStatusOptionViewModel>();
    }


    // =========================================================
    // RESULT STATUS
    // =========================================================

    public class ResultStatusOptionViewModel
    {
        public int Id { get; set; }

        public string StatusName { get; set; } = string.Empty;
    }


    // =========================================================
    // LOOKUP
    // =========================================================

    public class TestResultLookupViewModel
    {
        public int TestResultId { get; set; }

        public int AppointmentTestId { get; set; }

        public int AppointmentId { get; set; }

        public string? AppointmentCode { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan AppointmentTime { get; set; }


        // PATIENT

        public int PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }


        // TEST

        public int TestId { get; set; }

        public string? TestCode { get; set; }

        public string TestName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string? Unit { get; set; }

        public string? TurnAroundTime { get; set; }


        // REFERENCE

        public int ReferenceRangeId { get; set; }

        public string? ReferenceRange { get; set; }

        public string? ReferenceRangeDescription { get; set; }


        // SAMPLE

        public int SampleStatusId { get; set; }

        public string SampleStatusName { get; set; } = string.Empty;

        public int? SampleTypeId { get; set; }

        public string? SampleTypeName { get; set; }


        // RESULT

        public string? ResultValue { get; set; }

        public string? ResultInterpretation { get; set; }

        public string? Remarks { get; set; }

        public int? ResultStatusId { get; set; }

        public string? ResultStatusName { get; set; }

        public DateTime? ResultDate { get; set; }
    }


    // =========================================================
    // LAB USER
    // =========================================================

    public class LabUserLookupViewModel
    {
        public int Id { get; set; }

        public string? Email { get; set; }
    }
}