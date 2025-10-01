namespace Jobick.ViewModels;

public class ProjectViewModel
{
         public int Id { get; set; }  

        public string Name { get; set; }
        public string NameAr { get; set; }
        public string Description { get; set; }
        public string DescriptionAr { get; set; }
        public string ResponsibleForImplementing { get; set; }
        public string SystemOwner { get; set; }
        public string ProjectGoal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalCost { get; set; }
}
