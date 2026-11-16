using System.Reflection;
using NetArchTest.Rules;

namespace Tempest.Tests.Architecture;

/// <summary>Turns the Clean Architecture and Feature-Sliced Design rules into failing tests.</summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Tempest.Domain.Entities.Product).Assembly;
    private static readonly Assembly Application = typeof(Tempest.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Tempest.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly WebUI = typeof(Tempest.WebUI.App.DependencyInjection).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_any_other_layer_or_framework() =>
        AssertNoDependency(
            Types.InAssembly(Domain),
            "Tempest.Application", "Tempest.Infrastructure", "Tempest.WebUI",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "FluentValidation");

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_presentation() =>
        AssertNoDependency(
            Types.InAssembly(Application),
            "Tempest.Infrastructure", "Tempest.WebUI", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");

    [Fact]
    public void Infrastructure_does_not_depend_on_presentation() =>
        AssertNoDependency(Types.InAssembly(Infrastructure), "Tempest.WebUI");

    [Fact]
    public void Frontend_never_touches_domain_entities_or_persistence() =>
        AssertNoDependency(
            Types.InAssembly(WebUI),
            "Tempest.Domain.Entities", "Tempest.Domain.ValueObjects",
            "Tempest.Infrastructure.Persistence", "Microsoft.EntityFrameworkCore");

    [Theory]
    [InlineData("Tempest.WebUI.Shared", "Tempest.WebUI.Entities", "Tempest.WebUI.Features", "Tempest.WebUI.Widgets", "Tempest.WebUI.Pages", "Tempest.WebUI.App")]
    [InlineData("Tempest.WebUI.Entities", "Tempest.WebUI.Features", "Tempest.WebUI.Widgets", "Tempest.WebUI.Pages", "Tempest.WebUI.App")]
    [InlineData("Tempest.WebUI.Features", "Tempest.WebUI.Widgets", "Tempest.WebUI.Pages", "Tempest.WebUI.App")]
    [InlineData("Tempest.WebUI.Widgets", "Tempest.WebUI.Pages", "Tempest.WebUI.App")]
    public void Fsd_layers_only_import_from_lower_layers(string layer, params string[] higherLayers) =>
        AssertNoDependency(Types.InAssembly(WebUI).That().ResideInNamespaceStartingWith(layer), higherLayers);

    private static void AssertNoDependency(PredicateList types, params string[] forbidden) =>
        AssertResult(types.ShouldNot().HaveDependencyOnAny(forbidden).GetResult());

    private static void AssertNoDependency(Types types, params string[] forbidden) =>
        AssertResult(types.ShouldNot().HaveDependencyOnAny(forbidden).GetResult());

    private static void AssertResult(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            "Forbidden dependency found in: " + string.Join(", ", result.FailingTypeNames ?? []));
}
