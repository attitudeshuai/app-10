using AutoMapper;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;

namespace DeviceMaintenanceSystem.Helpers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();

        CreateMap<MaintenancePlan, MaintenancePlanDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.ResponsibleTechnicianName, opt => opt.MapFrom(s => s.ResponsibleTechnician != null ? s.ResponsibleTechnician.RealName : null))
            .ForMember(d => d.MaintenanceScheduleCode, opt => opt.MapFrom(s => s.MaintenanceSchedule != null ? s.MaintenanceSchedule.ScheduleCode : null));
        CreateMap<CreateMaintenancePlanDto, MaintenancePlan>();

        CreateMap<MaintenanceSchedule, MaintenanceScheduleDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.ResponsibleTechnicianName, opt => opt.MapFrom(s => s.ResponsibleTechnician != null ? s.ResponsibleTechnician.RealName : null));
        CreateMap<CreateMaintenanceScheduleDto, MaintenanceSchedule>();

        CreateMap<FaultReport, FaultReportDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.Reporter != null ? s.Reporter.RealName : null))
            .ForMember(d => d.AssignedTechnicianName, opt => opt.MapFrom(s => s.AssignedTechnician != null ? s.AssignedTechnician.RealName : null))
            .ForMember(d => d.SparePartConsumptions, opt => opt.MapFrom(s => s.SparePartConsumptions));
        CreateMap<CreateFaultReportDto, FaultReport>();

        CreateMap<SparePart, SparePartDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null));
        CreateMap<CreateSparePartDto, SparePart>();

        CreateMap<SparePartConsumption, SparePartConsumptionDto>()
            .ForMember(d => d.SparePartName, opt => opt.MapFrom(s => s.SparePart != null ? s.SparePart.Name : null))
            .ForMember(d => d.SparePartSpecification, opt => opt.MapFrom(s => s.SparePart != null ? s.SparePart.Specification : null))
            .ForMember(d => d.FaultReportCode, opt => opt.MapFrom(s => s.FaultReport != null ? s.FaultReport.ReportCode : null));

        CreateMap<Device, DeviceDetailDto>()
            .ForMember(d => d.RecentInspectionRecords, opt => opt.Ignore())
            .ForMember(d => d.InspectionRecordCount, opt => opt.Ignore())
            .ForMember(d => d.BorrowRecords, opt => opt.Ignore())
            .ForMember(d => d.BorrowRecordCount, opt => opt.Ignore())
            .ForMember(d => d.CurrentBorrowRecord, opt => opt.Ignore());

        CreateMap<CreateDeviceDto, Device>();

        CreateMap<InspectionPlan, InspectionPlanDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.AssignedTechnicianName, opt => opt.MapFrom(s => s.AssignedTechnician != null ? s.AssignedTechnician.RealName : null));
        CreateMap<CreateInspectionPlanDto, InspectionPlan>();

        CreateMap<InspectionTask, InspectionTaskDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.AssignedTechnicianName, opt => opt.MapFrom(s => s.AssignedTechnician != null ? s.AssignedTechnician.RealName : null))
            .ForMember(d => d.InspectionPlanCode, opt => opt.MapFrom(s => s.InspectionPlan != null ? s.InspectionPlan.PlanCode : null))
            .ForMember(d => d.InspectionPlanTitle, opt => opt.MapFrom(s => s.InspectionPlan != null ? s.InspectionPlan.Title : null))
            .ForMember(d => d.RecordCount, opt => opt.MapFrom(s => s.InspectionRecords != null ? s.InspectionRecords.Count : 0));

        CreateMap<InspectionRecord, InspectionRecordDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.InspectorName, opt => opt.MapFrom(s => s.Inspector != null ? s.Inspector.RealName : null))
            .ForMember(d => d.InspectionPlanCode, opt => opt.MapFrom(s => s.InspectionPlan != null ? s.InspectionPlan.PlanCode : null))
            .ForMember(d => d.InspectionTaskCode, opt => opt.MapFrom(s => s.InspectionTask != null ? s.InspectionTask.TaskCode : null))
            .ForMember(d => d.Photos, opt => opt.MapFrom(s => s.Photos));
        CreateMap<CreateInspectionRecordDto, InspectionRecord>();

        CreateMap<InspectionPhoto, InspectionPhotoDto>();

        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? s.User.RealName : null));
        CreateMap<CreateNotificationDto, Notification>();

        CreateMap<Supplier, SupplierDto>()
            .ForMember(d => d.DeviceCount, opt => opt.Ignore());
        CreateMap<Supplier, SupplierDetailDto>()
            .ForMember(d => d.DeviceCount, opt => opt.Ignore())
            .ForMember(d => d.Devices, opt => opt.Ignore());
        CreateMap<CreateSupplierDto, Supplier>();

        CreateMap<Device, SupplierDeviceDto>();

        CreateMap<Device, DeviceDto>()
            .ForMember(d => d.SupplierId, opt => opt.MapFrom(s => s.SupplierId))
            .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null));

        CreateMap<MaintenanceContract, MaintenanceContractDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
            .ForMember(d => d.Status, opt => opt.Ignore());

        CreateMap<MaintenanceContract, MaintenanceContractDetailDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.SupplierName, opt => opt.MapFrom(s => s.Supplier != null ? s.Supplier.Name : null))
            .ForMember(d => d.Status, opt => opt.Ignore());

        CreateMap<CreateMaintenanceContractDto, MaintenanceContract>();

        CreateMap<DeviceBorrowRecord, DeviceBorrowRecordDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.OperatorName, opt => opt.MapFrom(s => s.Operator != null ? s.Operator.RealName : null))
            .ForMember(d => d.ReturnOperatorName, opt => opt.MapFrom(s => s.ReturnOperator != null ? s.ReturnOperator.RealName : null))
            .ForMember(d => d.ApproverName, opt => opt.MapFrom(s => s.Approver != null ? s.Approver.RealName : null))
            .ForMember(d => d.ApplicantName, opt => opt.MapFrom(s => s.Applicant != null ? s.Applicant.RealName : null));
        CreateMap<CreateDeviceBorrowDto, DeviceBorrowRecord>();

        CreateMap<KnowledgeBaseArticle, KnowledgeBaseArticleDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.DeviceCategory, opt => opt.MapFrom(s => s.Device != null ? s.Device.Category : null))
            .ForMember(d => d.AuthorName, opt => opt.MapFrom(s => s.Author != null ? s.Author.RealName : null))
            .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.ArticleTags.Select(at => at.Tag).ToList()));

        CreateMap<KnowledgeBaseArticle, KnowledgeBaseArticleBriefDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCategory, opt => opt.MapFrom(s => s.Device != null ? s.Device.Category : null))
            .ForMember(d => d.AuthorName, opt => opt.MapFrom(s => s.Author != null ? s.Author.RealName : null))
            .ForMember(d => d.Tags, opt => opt.MapFrom(s => s.ArticleTags.Select(at => at.Tag).ToList()));

        CreateMap<CreateKnowledgeBaseArticleDto, KnowledgeBaseArticle>();

        CreateMap<Tag, TagDto>()
            .ForMember(d => d.ArticleCount, opt => opt.MapFrom(s => s.ArticleTags != null ? s.ArticleTags.Count : 0));

        CreateMap<CreateTagDto, Tag>();
    }
}
