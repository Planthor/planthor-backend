using System;
using System.Collections.Generic;
using Application.Dtos;

namespace Application.Tests.Dtos;

public class DtoConstructionTests
{
    [Fact]
    public void SportPlanDto_Construction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var sportTypes = new List<string> { "Run", "Ride" };

        var dto = new SportPlanDto(
            id, memberId, "Run 100km", "km", 100f, 50f,
            now, now.AddDays(30),
            "2026-01-01", "2026-12-31",
            "UTC", false, true, "PlanStatus_Planned_Desc", 5, sportTypes.AsReadOnly());

        Assert.Equal(id, dto.Id);
        Assert.Equal(memberId, dto.MemberId);
        Assert.Equal("Run 100km", dto.Name);
        Assert.Equal("km", dto.Unit);
        Assert.Equal(100f, dto.Target);
        Assert.Equal(50f, dto.CurrentValue);
        Assert.Equal(2, dto.SportTypes.Count);
    }

    [Fact]
    public void MemberDto_Construction_SetsAllProperties()
    {
        var id = Guid.NewGuid();

        var dto = new MemberDto(id, "Alice", "M", "Smith", "desc", "/avatar.jpg");

        Assert.Equal(id, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("M", dto.MiddleName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal("desc", dto.Description);
        Assert.Equal("/avatar.jpg", dto.PathAvatar);
    }

    [Fact]
    public void MemberDto_RecordEquality_EqualDtosAreEqual()
    {
        var id = Guid.NewGuid();
        var dto1 = new MemberDto(id, "Alice", "", "Smith", null, "");
        var dto2 = new MemberDto(id, "Alice", "", "Smith", null, "");

        Assert.Equal(dto1, dto2);
    }
}
