using System.Reflection;
using FinTrack.Domain.Accounts;
using FinTrack.Application.Accounts;
using FinTrack.Infrastructure;

namespace FinTrack.ArchitectureTests;

public sealed class LayerDependencyTests
{
  [Fact]
  public void Domain_HasNoDependenciesOnOtherLayersOrInfrastructureFrameworks()
  {
    var references = typeof(Account).Assembly.GetReferencedAssemblies();

    Assert.DoesNotContain(references, reference =>
      reference.Name!.StartsWith("FinTrack.") ||
      reference.Name.StartsWith("Microsoft.EntityFrameworkCore") ||
      reference.Name.StartsWith("Microsoft.AspNetCore") ||
      reference.Name.StartsWith("Npgsql"));
  }

  [Fact]
  public void Application_DependsOnlyOnDomainWithinSolution()
  {
    Assert.Equal(["FinTrack.Domain"], ProjectReferences(typeof(IAccountRepository).Assembly));
  }

  [Fact]
  public void Infrastructure_DependsOnApplicationAndDomain()
  {
    Assert.Equal(
      ["FinTrack.Application", "FinTrack.Domain"],
      ProjectReferences(typeof(DependencyInjection).Assembly)
    );
  }

  [Fact]
  public void Api_DependsOnApplicationAndInfrastructure()
  {
    Assert.Equal(
      ["FinTrack.Application", "FinTrack.Infrastructure"],
      ProjectReferences(typeof(Program).Assembly)
    );
  }

  private static string[] ProjectReferences(Assembly assembly) =>
    assembly.GetReferencedAssemblies()
      .Select(reference => reference.Name!)
      .Where(name => name.StartsWith("FinTrack."))
      .OrderBy(name => name)
      .ToArray();
}
