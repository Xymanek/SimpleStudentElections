using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using SimpleStudentElections.Helpers;

namespace SimpleStudentElections.Models.Forms
{
    [FormComplex]
    [Serializable]
    public class EmailForm
    {
        [UIHint("EmailSubject")]
        public string Subject { get; set; }
        
        [DataType(DataType.MultilineText), UIHint("CKEditor"), AllowHtml]
        public string Body { get; set; }
        
        public static ModelFieldsAccessibility DefaultFieldsInfo(ModelFieldsAccessibility.Kind? defaultKind = null)
        {
            return new ModelFieldsAccessibility()
            {
                DefaultKind = defaultKind
            };
        }
    }
}