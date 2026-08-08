using System.Text.Json.Serialization;

namespace Api.Requests;

/// <summary>
/// Abstract base request model for specific plan details.
/// Supports polymorphic deserialization based on the "type" property.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateSportPlanDetailsRequest), "sport")]
public abstract record CreatePlanDetailsRequest;
