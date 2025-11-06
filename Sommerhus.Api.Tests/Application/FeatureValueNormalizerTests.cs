//using FluentAssertions;
//using Sommerhus.Application.Admin.Houses;
//using Sommerhus.Contracts.Dtos.Admin.Features;
//using Sommerhus.Domain.Models;

//namespace Sommerhus.Api.Tests.Application;

//public sealed class FeatureValueNormalizerTests
//{
//    [Fact]
//    public void Normalize_IgnoresEmptyEntries()
//    {
//        var houseId = Guid.NewGuid();
//        var values = new List<PostFeatureValueDto>
//        {
//            new(Guid.Empty, null),
//            new(Guid.NewGuid(), "  "),
//            new(Guid.NewGuid(), null)
//        };

//        var normalized = FeatureValueNormalizer.Normalize(houseId, values);

//        normalized.Should().BeEmpty();
//    }

//    [Fact]
//    public void Normalize_DeduplicatesAndTrims()
//    {
//        var houseId = Guid.NewGuid();
//        var featureId = Guid.NewGuid();
//        var otherId = Guid.NewGuid();

//        var values = new List<PostFeatureValueDto>
//        {
//            new(featureId, "   First  "),
//            new(featureId, "Second"),
//            new(otherId, "  Value ")
//        };

//        var normalized = FeatureValueNormalizer.Normalize(houseId, values);

//        normalized.Should().HaveCount(2);
//        normalized.Should().ContainSingle(v => v.FeatureId == featureId)
//            .Which.RawValue.Should().Be("First");
//        normalized.Should().ContainSingle(v => v.FeatureId == otherId)
//            .Which.RawValue.Should().Be("Value");
//        normalized.All(v => v.HouseId == houseId).Should().BeTrue();
//    }
//}
