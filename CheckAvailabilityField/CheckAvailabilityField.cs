using System;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint;

namespace DanielBrown.SharePoint.CustomFields
{
    
	public class CheckAvailabilityField : Microsoft.SharePoint.SPField
	{
        public CheckAvailabilityField(SPFieldCollection fields, string fieldName)
            : base(fields, fieldName)
        {
        }
        
		public CheckAvailabilityField(SPFieldCollection fields, string typeName, string displayName)
            : base(fields, typeName, displayName)
        {
        }

        public override object GetFieldValue(string value)
        {
            return null;
        }

        public override BaseFieldControl FieldRenderingControl
        {
            get
            {
				BaseFieldControl control = new CheckAvailabilityFieldControl();
                control.FieldName = this.InternalName;
                return control;
            }
        }

		public override string GetValidatedString(object value)
		{
			if (value == null)
			{
                if (this.Required)
                {
                    throw new SPFieldValidationException("Invalid value for required field.");
                }
				return string.Empty;
			}
			else
			{
				return value.ToString();
			}
		}
	}
}
