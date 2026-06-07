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

        if (!await context.KnowledgeBaseArticles.AnyAsync())
        {
            var kbArticles = new List<KnowledgeBaseArticle>();
            var kbTitles = new[]
            {
                "轴承故障诊断与更换指南",
                "显示屏黑屏故障排查步骤",
                "电机过热保护机制及处理方法",
                "液压系统漏油常见原因及维修",
                "传动带更换与张紧调整",
                "传感器校准与故障诊断",
                "电源模块故障维修手册",
                "冷却系统维护与故障处理",
                "PLC程序故障排查方法",
                "气动系统压力不足故障诊断"
            };

            var kbSummaries = new[]
            {
                "详细介绍轴承故障的常见症状、诊断方法和更换步骤，帮助技术员快速定位和解决轴承相关问题。",
                "系统讲解显示屏黑屏故障的排查流程，从电源检查到模块更换，逐步定位故障点。",
                "深入分析电机过热的各种原因，介绍过热保护机制及相应的处理措施。",
                "总结液压系统漏油的常见原因，提供针对性的维修方案和预防措施。",
                "介绍传动带更换的标准流程，以及张紧度调整的方法和注意事项。",
                "讲解传感器常见故障类型、校准方法和故障诊断技巧。",
                "详细说明电源模块的工作原理、常见故障及维修方法。",
                "介绍冷却系统的日常维护要点和常见故障处理方法。",
                "系统讲解PLC程序故障的排查思路和常用诊断工具的使用。",
                "分析气动系统压力不足的常见原因，提供逐步诊断和解决方案。"
            };

            var kbContents = new[]
            {
                "## 轴承故障常见症状\n1. 异常噪音：嗡嗡声、尖锐声或摩擦声\n2. 振动加剧：设备运行时振动明显增大\n3. 温度升高：轴承部位温度异常升高\n\n## 诊断方法\n1. 听声判断：使用听音棒接触轴承部位\n2. 温度检测：使用红外测温仪测量温度\n3. 振动分析：使用振动分析仪检测振动频谱\n\n## 更换步骤\n1. 停机并断电，确保安全\n2. 拆卸轴承端盖和固定螺栓\n3. 使用拉马工具取出旧轴承\n4. 清洁轴承座和轴颈\n5. 安装新轴承，注意方向\n6. 添加适量润滑脂\n7. 复装端盖，调整间隙\n8. 试机运行，检查噪音和温度",

                "## 故障排查步骤\n### 第一步：检查电源\n1. 确认设备电源是否接通\n2. 检查电源指示灯是否亮起\n3. 测量电源输入电压是否正常\n\n### 第二步：检查连接线\n1. 检查显示屏信号线是否松动\n2. 检查电源线是否接触良好\n3. 检查接插件是否有氧化或损坏\n\n### 第三步：检查显示屏模块\n1. 更换已知正常的显示屏测试\n2. 检查显示屏背光是否工作\n3. 测量显示屏供电电压\n\n### 第四步：检查控制板\n1. 检查控制板输出信号\n2. 检查显示驱动芯片是否发热\n3. 必要时更换控制板",

                "## 电机过热常见原因\n1. 过载运行：负载超过额定值\n2. 散热不良：散热片堵塞、风扇损坏\n3. 电压异常：电压过高或过低\n4. 轴承故障：摩擦增大产生热量\n5. 绕组故障：匝间短路导致发热\n\n## 过热保护机制\n1. 温度传感器监测电机温度\n2. 达到预警温度时发出告警\n3. 达到保护温度时自动停机\n\n## 处理方法\n1. 立即停机，让电机自然冷却\n2. 检查负载是否过重，必要时减轻负载\n3. 清理散热片，检查冷却风扇\n4. 测量供电电压是否在正常范围\n5. 检查轴承润滑情况\n6. 如怀疑绕组故障，使用万用表检测",

                "## 常见漏油原因\n1. 密封件老化：使用时间长导致密封失效\n2. 接头松动：管接头螺纹松动\n3. 油封损坏：轴封磨损或损坏\n4. 油压过高：系统压力超过额定值\n5. 油液变质：油品老化导致密封性能下降\n\n## 维修方法\n1. 确定漏油部位：清洁后加压观察\n2. 接头漏油：紧固接头或更换密封圈\n3. 油封漏油：更换油封，检查轴表面\n4. 密封件漏油：更换相应密封件\n5. 检查系统压力，调整至正常范围\n\n## 预防措施\n1. 定期检查油位和油质\n2. 按周期更换密封件\n3. 保持系统清洁，防止污染\n4. 控制油温，避免过高",

                "## 更换前准备\n1. 确认传动带型号和规格\n2. 准备必要工具：扳手、张紧力计等\n3. 停机断电，挂牌警示\n\n## 更换步骤\n1. 松开电机固定螺栓\n2. 移动电机使传动带松弛\n3. 取下旧传动带，检查磨损情况\n4. 清洁带轮表面\n5. 安装新传动带\n6. 调整电机位置，初步张紧\n7. 使用张紧力计测量张紧度\n8. 紧固电机固定螺栓\n9. 手动转动设备，检查运转是否平稳\n\n## 张紧度调整\n1. 初张紧力按说明书要求\n2. 运行24小时后再次检查\n3. 根据使用周期定期检查张紧度\n4. 过松会打滑，过紧会加速磨损",

                "## 常见故障类型\n1. 无输出信号\n2. 输出信号不稳定\n3. 测量值偏差大\n4. 响应迟缓\n\n## 校准方法\n### 零点校准\n1. 将传感器置于零输入状态\n2. 测量输出信号\n3. 调整零点电位器或软件校准\n\n### 量程校准\n1. 输入标准满量程信号\n2. 测量输出值\n3. 调整增益使其符合要求\n\n## 故障诊断\n1. 检查供电电压是否正常\n2. 检查接线是否松动或接触不良\n3. 检查传感器表面是否有损坏或污染\n4. 使用标准信号源测试传感器响应\n5. 对比同型号正常传感器判断是否损坏",

                "## 电源模块工作原理\n电源模块将输入交流电转换为稳定的直流电供给设备各部分使用。主要包括整流、滤波、稳压等环节。\n\n## 常见故障\n### 1. 无输出\n- 保险丝熔断\n- 输入电路故障\n- 开关管损坏\n\n### 2. 输出电压不稳定\n- 稳压电路故障\n- 负载波动过大\n- 滤波电容失效\n\n### 3. 输出电压偏高或偏低\n- 参考电压偏移\n- 反馈电路故障\n\n## 维修步骤\n1. 检查输入电压是否正常\n2. 检查保险丝是否完好\n3. 检查输出端是否有短路\n4. 测量各关键测试点电压\n5. 更换损坏的元器件\n6. 通电测试，验证输出稳定性",

                "## 日常维护要点\n1. 定期检查冷却液液位\n2. 定期清洗冷却器散热片\n3. 检查冷却风扇运转是否正常\n4. 监测冷却系统压力\n5. 定期更换冷却液\n\n## 常见故障及处理\n### 温度过高\n1. 检查冷却液液位，不足时添加\n2. 检查冷却器是否堵塞，进行清洗\n3. 检查冷却风扇是否工作\n4. 检查水泵运转是否正常\n5. 检查是否有漏水现象\n\n### 压力不足\n1. 检查是否有泄漏点\n2. 检查水泵工作状态\n3. 检查压力传感器是否准确\n\n### 冷却液泄漏\n1. 查找泄漏点\n2. 紧固松动的接头\n3. 更换损坏的密封件\n4. 补充冷却液并排空系统空气",

                "## 故障排查思路\n### 1. 确认故障现象\n- 具体是什么故障？\n- 故障是如何发生的？\n- 是否有报警信息？\n\n### 2. 检查硬件连接\n- 电源是否正常\n- 通讯线是否连接良好\n- IO模块是否正常\n- 输入输出信号是否正确\n\n### 3. 检查程序状态\n- 程序是否在运行\n- 是否有故障代码\n- 监视关键寄存器状态\n\n### 4. 逐步排查\n- 从输入到输出逐步检查\n- 对比正常运行时的状态\n- 使用强制功能测试输出\n\n## 常用诊断工具\n1. PLC编程软件：在线监视程序\n2. 万用表：测量电压、电阻\n3. 信号发生器：模拟输入信号\n4. 示波器：观测信号波形",

                "## 故障诊断步骤\n### 第一步：检查气源\n1. 确认空压机是否正常运行\n2. 检查气源压力是否达标\n3. 检查空气过滤器是否堵塞\n\n### 第二步：检查主管路\n1. 检查主管路是否有泄漏\n2. 检查减压阀是否正常工作\n3. 检查干燥器是否正常\n\n### 第三步：检查支路\n1. 检查各支路阀门是否打开\n2. 检查支路过滤器状态\n3. 检查各支路是否有泄漏\n\n### 第四步：检查执行元件\n1. 检查气缸密封是否良好\n2. 检查电磁阀是否正常换向\n3. 检查速度控制阀是否调节得当\n\n## 常见泄漏点检查\n1. 管接头处\n2. 电磁阀阀岛\n3. 气缸活塞杆密封处\n4. 各种阀体连接处\n5. 软管老化破损处"
            };

            var kbKeywords = new[]
            {
                "轴承,噪音,振动,更换,润滑",
                "显示屏,黑屏,电源,模块,维修",
                "电机,过热,温度,保护,散热",
                "液压,漏油,密封,油压,维修",
                "传动带,更换,张紧,调整,磨损",
                "传感器,校准,故障,信号,测量",
                "电源,模块,电压,稳压,维修",
                "冷却,温度,压力,冷却液,维护",
                "PLC,程序,故障,诊断,IO",
                "气动,压力,泄漏,气缸,电磁阀"
            };

            var technicianId = users.First(u => u.Role == UserRole.Technician).Id;

            for (int i = 0; i < kbTitles.Length; i++)
            {
                var device = devices[i % devices.Count];
                var article = new KnowledgeBaseArticle
                {
                    ArticleCode = $"KB-20240101-{i + 1:D4}",
                    Title = kbTitles[i],
                    Summary = kbSummaries[i],
                    Content = kbContents[i],
                    Keywords = kbKeywords[i],
                    DeviceId = device.Id,
                    AuthorId = technicianId,
                    Status = KnowledgeBaseStatus.Published,
                    ViewCount = random.Next(10, 200),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30, 180)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                };
                kbArticles.Add(article);
            }

            await context.KnowledgeBaseArticles.AddRangeAsync(kbArticles);
            await context.SaveChangesAsync();

            if (!await context.Tags.AnyAsync())
            {
                var tags = new List<Tag>
                {
                    new() { Name = "机械故障", Type = TagType.FaultType, Color = "#ef4444", SortOrder = 1 },
                    new() { Name = "电气故障", Type = TagType.FaultType, Color = "#f59e0b", SortOrder = 2 },
                    new() { Name = "液压故障", Type = TagType.FaultType, Color = "#3b82f6", SortOrder = 3 },
                    new() { Name = "气动故障", Type = TagType.FaultType, Color = "#8b5cf6", SortOrder = 4 },
                    new() { Name = "控制系统", Type = TagType.FaultType, Color = "#10b981", SortOrder = 5 },
                    new() { Name = "生产设备", Type = TagType.DeviceCategory, Color = "#06b6d4", SortOrder = 10 },
                    new() { Name = "办公设备", Type = TagType.DeviceCategory, Color = "#6366f1", SortOrder = 11 },
                    new() { Name = "IT设备", Type = TagType.DeviceCategory, Color = "#ec4899", SortOrder = 12 },
                    new() { Name = "检测设备", Type = TagType.DeviceCategory, Color = "#14b8a6", SortOrder = 13 },
                    new() { Name = "运输设备", Type = TagType.DeviceCategory, Color = "#f97316", SortOrder = 14 },
                    new() { Name = "日常维护", Type = TagType.Custom, Color = "#22c55e", SortOrder = 20 },
                    new() { Name = "故障排查", Type = TagType.Custom, Color = "#ef4444", SortOrder = 21 },
                    new() { Name = "更换指南", Type = TagType.Custom, Color = "#3b82f6", SortOrder = 22 },
                    new() { Name = "安全操作", Type = TagType.Custom, Color = "#f59e0b", SortOrder = 23 },
                    new() { Name = "保养技巧", Type = TagType.Custom, Color = "#8b5cf6", SortOrder = 24 }
                };

                await context.Tags.AddRangeAsync(tags);
                await context.SaveChangesAsync();

                var faultTypeTags = tags.Where(t => t.Type == TagType.FaultType).ToList();
                var customTags = tags.Where(t => t.Type == TagType.Custom).ToList();

                var articleTagMappings = new[]
                {
                    new[] { "机械故障", "更换指南", "日常维护" },
                    new[] { "电气故障", "故障排查" },
                    new[] { "电气故障", "日常维护", "保养技巧" },
                    new[] { "液压故障", "故障排查" },
                    new[] { "机械故障", "更换指南", "保养技巧" },
                    new[] { "控制系统", "故障排查", "日常维护" },
                    new[] { "电气故障", "故障排查", "更换指南" },
                    new[] { "日常维护", "保养技巧" },
                    new[] { "控制系统", "故障排查" },
                    new[] { "气动故障", "故障排查" }
                };

                var articleTagList = new List<KnowledgeBaseArticleTag>();
                for (int i = 0; i < kbArticles.Count && i < articleTagMappings.Length; i++)
                {
                    var article = kbArticles[i];
                    var mappingNames = articleTagMappings[i];
                    var matchingTags = tags.Where(t => mappingNames.Contains(t.Name)).ToList();
                    foreach (var tag in matchingTags)
                    {
                        articleTagList.Add(new KnowledgeBaseArticleTag
                        {
                            ArticleId = article.Id,
                            TagId = tag.Id
                        });
                    }
                }

                await context.KnowledgeBaseArticleTags.AddRangeAsync(articleTagList);
                await context.SaveChangesAsync();
            }
        }
    }
}
