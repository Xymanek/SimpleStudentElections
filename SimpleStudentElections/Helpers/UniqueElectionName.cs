using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SimpleStudentElections.Models;
using SimpleStudentElections.Models.Forms;

namespace SimpleStudentElections.Helpers
{
    /// <summary>
    /// A validation rule for <see cref="CouncilElectionData"/> to check that the name is unique 
    /// </summary>
    public class UniqueElectionName : ValidationAttribute
    {
        public UniqueElectionName()
        {
            ErrorMessage = "Must be unique";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            VotingDbContext db = new VotingDbContext();
            string name = (string) value;
            ElectionForm form;
            
            try
            {
                form = (ElectionForm) validationContext.ObjectInstance;
            }
            catch (InvalidCastException e)
            {
                throw new Exception($"{nameof(UniqueElectionName)} only supports {nameof(ElectionForm)}", e);
            }

            IQueryable<Election> query = db.Elections.Where(election => election.Name == name);

            if (form.Id != null)
            {
                int id = form.Id.Value;
                query = query.Where(election => election.Id != id);
            }

            return query.Any()
                ? new ValidationResult(ErrorMessageString)
                : ValidationResult.Success;
        }
    }
}