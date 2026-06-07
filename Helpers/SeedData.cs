using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Helpers;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Users.AnyAsync())
        {
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        var users = new List<User>
        {
            new()
            {
                Username = "admin",
                PasswordHash = passwordHash,
                RealName = "系统管理员",
                Email = "admin@example.com",
                Phone = "13800000001",
                Role = UserRole.Admin,
                IsActive = true
            },
            new()
            {
                Username = "user1",
                PasswordHash = passwordHash,
                RealName = "张三",
                Email = "zhangsan@example.com",
                Phone = "13800000002",
                Role = UserRole.User,
                IsActive = true
            },
            new()
            {
                Username = "user2",
                PasswordHash = passwordHash,
                RealName = "李四",
                Email = "lisi@example.com",
                Phone = "13800000003",
                Role = UserRole.User,
                IsActive = true
            },
            new()
            {
                Username = "tech1",
                PasswordHash = passwordHash,
                RealName = "王工程师",
                Email = "wang@example.com",
                Phone = "13900000001",
                Role = UserRole.Technician,
                IsActive = true
            },
            new()
            {
                Username = "tech2",
                PasswordHash = passwordHash,
                RealName = "刘工程师",
                Email = "liu@example.com",
                Phone = "13900000002",
                Role = UserRole.Technician,
                IsActive = true
            },
            new()
            {
                Username = "tech3",
                PasswordHash = passwordHash,
                RealName = "陈工程师",
                Email = "chen@example.com",
                Phone = "13900000003",
                Role = UserRole.Technician,
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        var categories = new[] { "生产设备", "办公设备", "IT设备", "检测设备", "运输设备" };
        var manufacturers = new[] { "华为技术", "西门子", "三菱电机", "施耐德", "ABB", "欧姆龙" };
        var locations = new[] { "一号车间", "二号车间", "办公楼", "研发中心", "仓库" };
        var deviceNames = new[]
        {
            "数控车床", "立式加工中心", "卧式铣床", "磨床", "钻床",
            "激光切割机", "注塑机", "冲压机", "焊接机器人", "包装生产线",
            "空气压缩机", "中央空调", "发电机组", "变压器", "配电柜",
            "服务器", "交换机", "打印机", "复印机", "投影仪"
        };

        var devices = new List<Device>();
        var random = new Random(42);

        for (int i = 0; i < 30; i++)
        {
            var device = new Device
            {
                DeviceCode = $"DEV-{i + 1:D4}",
                Name = deviceNames[i % deviceNames.Length] + (i >= deviceNames.Length ? $" #{i / deviceNames.Length + 1}" : ""),
                Category = categories[random.Next(categories.Length)],
                Model = $"Model-{random.Next(100, 999)}",
                Manufacturer = manufacturers[random.Next(manufacturers.Length)],
                PurchaseDate = DateTime.UtcNow.AddDays(-random.Next(30, 1000)),
                PurchasePrice = (decimal)(random.Next(5000, 500000) + random.NextDouble()),
                Location = locations[random.Next(locations.Length)],
                Status = (DeviceStatus)random.Next(0, 4),
                Description = $"这是一台{deviceNames[i % deviceNames.Length]}，用于生产加工。设备编号：DEV-{i + 1:D4}"
            };
            devices.Add(device);
        }

        await context.Devices.AddRangeAsync(devices);
        await context.SaveChangesAsync();

        var technicianIds = users.Where(u => u.Role == UserRole.Technician).Select(u => u.Id).ToList();
        var userIds = users.Where(u => u.Role == UserRole.User).Select(u => u.Id).ToList();
        var cycleValues = Enum.GetValues<MaintenanceCycle>();

        var maintenancePlans = new List<MaintenancePlan>();
        for (int i = 0; i < 20; i++)
        {
            var plan = new MaintenancePlan
            {
                PlanCode = $"MP-{i + 1:D4}",
                Title = $"{devices[i % devices.Count].Name}保养计划",
                DeviceId = devices[i % devices.Count].Id,
                Cycle = cycleValues[random.Next(cycleValues.Length)],
                PlannedDate = DateTime.UtcNow.AddDays(-random.Next(10, 60)).AddHours(random.Next(8, 18)),
                ResponsibleTechnicianId = technicianIds[random.Next(technicianIds.Count)],
                Content = $"对{devices[i % devices.Count].Name}进行定期保养维护，包括清洁、润滑、检查、更换易损件等工作。确保设备正常运行，延长设备使用寿命。",
                Status = (MaintenancePlanStatus)random.Next(0, 4),
                Remark = "请按时执行保养任务"
            };

            if (plan.Status == MaintenancePlanStatus.InProgress)
            {
                plan.ActualStartDate = plan.PlannedDate.AddHours(random.Next(0, 2));
            }
            else if (plan.Status == MaintenancePlanStatus.Completed)
            {
                plan.ActualStartDate = plan.PlannedDate.AddHours(random.Next(0, 2));
                plan.ActualEndDate = plan.ActualStartDate.Value.AddHours(random.Next(2, 8));
                plan.Result = "保养完成，设备运行正常，所有检查项均符合标准要求。";
            }

            maintenancePlans.Add(plan);
        }

        await context.MaintenancePlans.AddRangeAsync(maintenancePlans);
        await context.SaveChangesAsync();

        var faultPriorities = Enum.GetValues<FaultPriority>();
        var faultStatuses = Enum.GetValues<FaultStatus>();

        var faultReports = new List<FaultReport>();
        var faultDescriptions = new[]
        {
            "设备运行时出现异常噪音，疑似轴承故障",
            "显示屏无显示，操作面板失灵",
            "电机温度过高，自动停机保护",
            "液压系统漏油严重",
            "传动带断裂，无法正常运转",
            "传感器读数异常，数据不准",
            "电源模块故障，无法启动",
            "冷却系统故障，温度过高",
            "PLC程序错误，动作混乱",
            "气动系统压力不足"
        };

        var faultSolutions = new[]
        {
            "更换轴承，添加润滑脂，设备恢复正常",
            "更换显示屏模块，重新校准",
            "清理散热片，更换风扇，温度恢复正常",
            "更换密封件，补充液压油",
            "更换传动带，调整张紧度",
            "更换传感器，重新校准零点",
            "更换电源模块，检查供电线路",
            "清洗冷却器，补充冷却液",
            "重新下载PLC程序，检查IO模块",
            "修复漏气点，更换气动元件"
        };

        for (int i = 0; i < 25; i++)
        {
            var device = devices[random.Next(devices.Count)];
            var report = new FaultReport
            {
                ReportCode = $"FR-{i + 1:D4}",
                Title = $"{device.Name}故障报修",
                DeviceId = device.Id,
                ReporterId = userIds[random.Next(userIds.Count)],
                Priority = faultPriorities[random.Next(faultPriorities.Length)],
                Status = faultStatuses[random.Next(faultStatuses.Length - 1)],
                Description = faultDescriptions[random.Next(faultDescriptions.Length)],
                FaultLocation = device.Location,
                ReportTime = DateTime.UtcNow.AddDays(-random.Next(1, 30)).AddHours(random.Next(8, 20))
            };

            if (report.Status >= FaultStatus.Assigned && report.Status != FaultStatus.Cancelled)
            {
                report.AssignedTechnicianId = technicianIds[random.Next(technicianIds.Count)];
                report.AssignTime = report.ReportTime.AddMinutes(random.Next(10, 120));

                if (report.Status >= FaultStatus.InProgress)
                {
                    report.StartTime = report.AssignTime.Value.AddMinutes(random.Next(20, 180));

                    if (report.Status == FaultStatus.Completed)
                    {
                        report.CompleteTime = report.StartTime.Value.AddHours(random.Next(1, 12));
                        report.Solution = faultSolutions[random.Next(faultSolutions.Length)];
                        report.Remark = "故障已排除，设备恢复正常运行";
                    }
                }
            }

            if (report.Status == FaultStatus.Cancelled)
            {
                report.Remark = "误报，设备实际正常";
            }

            faultReports.Add(report);
        }

        await context.FaultReports.AddRangeAsync(faultReports);
        await context.SaveChangesAsync();
    }
}
