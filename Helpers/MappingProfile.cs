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

        CreateMap<Device, DeviceDto>();
        CreateMap<CreateDeviceDto, Device>();

        CreateMap<MaintenancePlan, MaintenancePlanDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.ResponsibleTechnicianName, opt => opt.MapFrom(s => s.ResponsibleTechnician != null ? s.ResponsibleTechnician.RealName : null));
        CreateMap<CreateMaintenancePlanDto, MaintenancePlan>();

        CreateMap<FaultReport, FaultReportDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null))
            .ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.Reporter != null ? s.Reporter.RealName : null))
            .ForMember(d => d.AssignedTechnicianName, opt => opt.MapFrom(s => s.AssignedTechnician != null ? s.AssignedTechnician.RealName : null));
        CreateMap<CreateFaultReportDto, FaultReport>();

        CreateMap<SparePart, SparePartDto>()
            .ForMember(d => d.DeviceName, opt => opt.MapFrom(s => s.Device != null ? s.Device.Name : null))
            .ForMember(d => d.DeviceCode, opt => opt.MapFrom(s => s.Device != null ? s.Device.DeviceCode : null));
        CreateMap<CreateSparePartDto, SparePart>();

        CreateMap<SparePartConsumption, SparePartConsumptionDto>()
            .ForMember(d => d.SparePartName, opt => opt.MapFrom(s => s.SparePart != null ? s.SparePart.Name : null))
            .ForMember(d => d.SparePartSpecification, opt => opt.MapFrom(s => s.SparePart != null ? s.SparePart.Specification : null))
            .ForMember(d => d.FaultReportCode, opt => opt.MapFrom(s => s.FaultReport != null ? s.FaultReport.ReportCode : null));

        CreateMap<Device, DeviceDto>();
        CreateMap<Device, DeviceDetailDto>()
            .ForMember(d => d.RecentInspectionRecords, opt => opt.Ignore())
            .ForMember(d => d.InspectionRecordCount, opt => opt.Ignore());

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
    }
}
