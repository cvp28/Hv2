
namespace Hv2UI;

/// <summary>
/// A simple option type intended for Hv2, but designed with general use in mind
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Maybe<T>
{
	public T Result;
	public bool Succeeded;

	public Maybe(T Result, bool Succeeded)
	{
		this.Result = Result;
		this.Succeeded = Succeeded;
	}

	public static Maybe<T> Success(T Result) => new(Result, true);
	public static Maybe<T> Fail() => new(default, false);

	public static implicit operator T(Maybe<T> other) => other.Result;
	public static implicit operator bool(Maybe<T> other) => other.Succeeded;
}
