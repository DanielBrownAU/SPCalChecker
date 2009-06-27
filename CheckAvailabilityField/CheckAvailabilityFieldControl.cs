// Using Statements
using System;
using System.Web.UI.WebControls;

// SharePoint Using Statements
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using System.Collections;
using System.Collections.Generic;


namespace DanielBrown.SharePoint.CustomFields
{
    /// <summary>
    /// Provides a Check Availability button which checks if there are any conflicting appointments in the calender.
    /// </summary>
    public class CheckAvailabilityFieldControl : BaseFieldControl, IDesignTimeHtmlProvider
    {
        #region Members
        // Button Instance
        private Button btnCheckAvailability;

        // Field names
        private string EventDateFieldName = "EventDate";
        private string EndDateFieldName = "EndDate";

        // Get the Event Start Dtae & Time
        private DateTime EventStartDate = default(DateTime);

        //Get the Events End Date & Time
        private DateTime EventEndDate = default(DateTime);

        private readonly int MAX_PEOPLE_PER_SINGLE_HOUR = 100;
        private readonly int MAX_PEOPLE_PER_BOOKING = 33;
        #endregion

        /// <summary>
        /// Creates a instance of a button if it is null. Sets ID, Text and Event Handler and adds it to the Controls Collection
        /// The button does not trigger Validation
        /// </summary>
        private void CreateButton()
        {
            // Check of the instance is null
            if (this.btnCheckAvailability == null)
            {
                // Create a new Button Instance
                this.btnCheckAvailability = new Button();

                // Default button Text
                string ButtonText = "Check Availability";

                // Get the CheckAvailabilityButtonText appseting from web.config
                string CheckAvailabilityButtonText = System.Configuration.ConfigurationSettings.AppSettings["CheckAvailabilityButtonText"];

                // Check if it is not null or empty
                if (!string.IsNullOrEmpty(CheckAvailabilityButtonText))
                {
                    // Use the appsetting value for the buttons text
                    ButtonText = CheckAvailabilityButtonText;
                }

                // Assign the text
                this.btnCheckAvailability.Text = ButtonText;

                // Set Causes Validation:
                this.btnCheckAvailability.CausesValidation = false;

                // Click Event
                this.btnCheckAvailability.Click += new EventHandler(btnCheckAvailability_Click);
            }

            // Att the button to our controls collection
            this.Controls.Add(this.btnCheckAvailability);
        }

        /// <summary>
        /// Checks to see if there are any Bad Day Conflicts for the time of this item. If found, they are added to a array which is returned
        /// </summary>
        /// <returns>A array of conflicting bad day bookings</returns>
        IList<SPListItem> GetBadDayConflicts()
        {
            IList<SPListItem> items = new List<SPListItem>();

            // New Query Instance
            SPQuery Query = this.BuildCAMLQuery();

            // Run the Query and get the list items
            SPListItemCollection ExistingItems = this.ListItem.ParentList.GetItems(Query);

            // Loop though the existing items.
            foreach (SPListItem ExistingEvent in ExistingItems)
            {
                // Get the Existing Items EventStart
                DateTime ExistingEvent_EventDate = (DateTime)ExistingEvent[this.EventDateFieldName];

                // Get the Existing Items EventEnd
                DateTime ExistingEvent_EventEnd = (DateTime)ExistingEvent[this.EndDateFieldName];

                // default Unavaialble Day Content Type Name
                string UnavilableDayContentTypeName = "Mark Day Unavailable";

                // Get the Appsetting
                string BadDayContentTypeName = System.Configuration.ConfigurationSettings.AppSettings["BadDayContentTypeName"];

                // Check if it is not null or empty
                if (!string.IsNullOrEmpty(BadDayContentTypeName))
                {
                    // Use the appsetting value for the content type name
                    UnavilableDayContentTypeName = BadDayContentTypeName;
                }

                if (ExistingEvent["Content Type"].ToString().ToLower() == UnavilableDayContentTypeName.ToLower())
                {
                    if ((this.EventStartDate.Date == ExistingEvent_EventDate.Date) || (this.EventEndDate.Date == ExistingEvent_EventEnd.Date))
                    {
                        items.Add(ExistingEvent);
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Checks to see if there are any Conflicts for the time of this item. If found, they are added to a array which is returned
        /// </summary>
        /// <returns>A array of conflicting bookings</returns>
        IList<SPListItem> GetConflicts()
        {
            IList<SPListItem> items = new List<SPListItem>();

            // New Query Instance
            SPQuery Query = this.BuildCAMLQuery();

            // Run the Query and get the list items
            SPListItemCollection ExistingItems = this.ListItem.ParentList.GetItems(Query);

            // Loop though the existing items.
            foreach (SPListItem ExistingEvent in ExistingItems)
            {
                // Dont include ourself in our conflicts
                if (ExistingEvent.ID == this.Item.ID)
                {
                    continue;
                }
                // Get the Existing Items EventStart
                DateTime ExistingEvent_EventDate = (DateTime)ExistingEvent[this.EventDateFieldName];

                // Get the Existing Items EventEnd
                DateTime ExistingEvent_EventEnd = (DateTime)ExistingEvent[this.EndDateFieldName];

                // If the EventDate is greator or equal to the Existing events EventEnd OR the EndEvent is less than or equal to the Existing items EventDate
                if ((this.EventStartDate >= ExistingEvent_EventEnd) || (this.EventEndDate <= ExistingEvent_EventDate))
                {
                    continue; // continue though
                }
                else
                {
                    items.Add(ExistingEvent);
                }
            }

            return items;
        }

        /// <summary>
        /// Checks if there are any Bad Fay Conflcits
        /// </summary>
        /// <returns>TRUE if there is, otherwise FALSE</returns>
        private bool HasBadDayConflict()
        {
            IList<SPListItem> items = this.GetBadDayConflicts();

            return (items.Count > 0);
        }

        /// <summary>
        /// Check Availability Button Click Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCheckAvailability_Click(object sender, EventArgs e)
        {
            // Error message
            string ErrorMessage = string.Empty;

            // Check if the EventDate Field is null
            bool DoesEventDateFieldExists = !this.IsFieldNull(EventDateFieldName);

            // For this check to work, we need both fields
            if ((DoesEventDateFieldExists) && (!IsFieldNull(EndDateFieldName)))
            {
                // Get the Event Start Dtae & Time
                EventStartDate = (DateTime)this.Item[EventDateFieldName];

                //Get the Events End Date & Time
                EventEndDate = (DateTime)this.Item[EndDateFieldName];

                if (this.HasBadDayConflict())
                {
                    ErrorMessage = "The booking cannot be made as the requested date has been marked as unavilable. Please select another date and try again.";
                }
                else if (!this.IsTimeFree()) // Check if the items time is free or has conflicting appointments
                {
                    // Conflict found, Set error message
                    ErrorMessage = "This appointment conflicts with an existing appointment";
                }
                else
                {
                    
                }
                
            }
            else
            {
                // Check if we dont have EventStart field
                if (!DoesEventDateFieldExists)
                {
                    // Set error message
                    ErrorMessage = "The EventDate (\"Start Date\") field could not be found.";
                }

                // Check if we dont have EventEnd field
                if (!this.List.Fields.ContainsField(EndDateFieldName))
                {
                    if (!DoesEventDateFieldExists) // Another check to put in the \r\n
                    {
                        ErrorMessage = string.Format("{0}\r\n", ErrorMessage);
                    }

                    ErrorMessage = string.Format("{0}The EndDate (\"End Date\") field could not be found.", ErrorMessage);
                }
            }
            
            // Register the javscript on the page
            this.RegisterScript(ErrorMessage);
        }

        /// <summary>
        /// Registers teh Javascript on the page
        /// </summary>
        /// <param name="ErrorMessage">The message to display in the alert window</param>
        private void RegisterScript(string ErrorMessage)
        {
            // if the length is greater than 0
            if (ErrorMessage.Length > 0)
            {
                // Our Script Key
                string ScriptKey = "ChkCalAvaBtn";

                // Check that its not already registered
                if (!this.Page.ClientScript.IsStartupScriptRegistered(ScriptKey))
                {
                    // Create the Script tag
                    string error = string.Format("<script type=\"text/javascript\">javascript:alert('{0}');</script>", ErrorMessage);

                    // Register the script as a StartUp Script
                    this.Page.ClientScript.RegisterStartupScript(this.GetType(), ScriptKey, error);
                }
            }
        }

        /// <summary>
        /// Checks to see if the EventStart and EventDate time for this item does not coincide with a existing booking
        /// </summary>
        /// <returns>TRUE if there are no conflicting bookings, otherwise FALSE</returns>
        private bool IsTimeFree()
        {
            IList<SPListItem> items = this.GetConflicts();

            // Is items Less than or equal to 0?
            return (items.Count <= 0);
        }

        /// <summary>
        /// Builds and executes a CAML query to get the list items in the calender to check for duplicates
        /// </summary>
        /// <returns>A SPQuery Object</returns>
        private SPQuery BuildCAMLQuery()
        {
            // Create a new SPQuery instance
            SPQuery query = new SPQuery();
            query.ExpandRecurrence = true;
            DateTime queryDate = this.EventStartDate;
            string queryPeriod = "";

            // If the Year matches
            if (this.EventStartDate.Year == this.EventEndDate.Year)
            {
                // If the Month Matches
                if (this.EventStartDate.Month == this.EventEndDate.Month)
                {
                    // If the Day Matches
                    if (this.EventStartDate.Day == this.EventEndDate.Day)
                    {
                        // Use week
                        queryPeriod = "<Week />";
                    }
                    else
                    {
                        // Fall back to Month
                        queryDate = new DateTime(this.EventStartDate.Year, this.EventStartDate.Month, 1);
                        queryPeriod = "<Month />";
                    }
                }
                else
                {
                    queryDate = new DateTime(this.EventStartDate.Year, this.EventStartDate.Month, 1);
                    
                    if (queryDate < this.EventEndDate)
                    {
                        // Use Month
                        queryPeriod = "<Month />";
                    }
                }
            }
            else
            {
                queryDate = new DateTime(this.EventStartDate.Year, this.EventStartDate.Month, 1);
                
                // Ifthe query date is less than the EvnetDate
                if (queryDate < this.EventEndDate)
                {
                    // Use Month
                    queryPeriod = "<Month />";
                }
            }
            // Sert the CalendarDate
            query.CalendarDate = queryDate;

            // Set the actual CAML query
            query.Query = string.Format("<Where><DateRangesOverlap><FieldRef Name=\"EventDate\" /><FieldRef Name=\"EndDate\" /><FieldRef Name=\"RecurrenceID\" /><Value Type=\"DateTime\">{0}</Value></DateRangesOverlap></Where>", queryPeriod);

            // return the query
            return query;
        }

        /// <summary>
        /// Checks if the Item's field is null
        /// </summary>
        /// <param name="name">The name of the Field to check the value of</param>
        /// <returns>If the field is null true is returned, otherwise fale</returns>
        private bool IsFieldNull(string name)
        {
            // Reutrn if the value is null or not
            return (this.Item[name] == null);
        }

        /// <summary>
        /// Overridden CreateChildControls to create the field :)
        /// </summary>
        protected override void CreateChildControls()
        {
            // New and Edit mode Only
            if ((this.ControlMode == SPControlMode.New) || (this.ControlMode == SPControlMode.Edit))
            {
                // Create our base child controls
                base.CreateChildControls();

                // Create the button
                this.CreateButton();
            }
        }

        /// <summary>
        /// Return nothing.
        /// </summary>
        public override object Value
        {
            get
            {
                // return nothing
                return null;
            }
            set
            {
                // set nothing
            }
        }
    }
}