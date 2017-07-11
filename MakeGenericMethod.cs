using System;
using System.Reflection;
using System.Collections.Generic;

public static class TypeExtensions
{
    public static MethodInfo MakeGenericMethod(this Type type, string name, Type[] genericTypeArguments)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (genericTypeArguments == null)
            throw new ArgumentNullException(nameof(genericTypeArguments));

        return type.MakeGenericMethodImpl(name, genericTypeArguments, parameterTypes: null);
    }

    public static MethodInfo MakeGenericMethod(this Type type, string name, Type[] genericTypeArguments, Type[] parameterTypes)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (genericTypeArguments == null)
            throw new ArgumentNullException(nameof(genericTypeArguments));
        if (parameterTypes == null)
            throw new ArgumentNullException(nameof(parameterTypes));
        foreach (Type parameterType in parameterTypes)
        {
            if (parameterType == null)
                throw new ArgumentNullException(nameof(parameterTypes));
        }

        return type.MakeGenericMethodImpl(name, genericTypeArguments, parameterTypes);
    }

    private static MethodInfo MakeGenericMethodImpl(this Type type, string name, Type[] genericTypeArguments, Type[] parameterTypes)
    {
        using (IEnumerator<MethodInfo> e = FindCandidates(type, name, genericTypeArguments, parameterTypes).GetEnumerator())
        {
            if (!e.MoveNext())
                return null;
            MethodInfo match = e.Current;
            if (e.MoveNext())
                throw new AmbiguousMatchException();
            return match.MakeGenericMethod(genericTypeArguments);
        }
    }

    private static IEnumerable<MethodInfo> FindCandidates(Type type, string name, Type[] genericTypeArguments, Type[] expectedParameterTypes)
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding;
        int expectedGenericArity = genericTypeArguments.Length;
        foreach (MethodInfo candidate in type.GetMember(name, MemberTypes.Method, bindingFlags))
        {
            if (!candidate.IsGenericMethodDefinition)
                continue;

            if (candidate.GetGenericArguments().Length != expectedGenericArity)
                continue;

            if (expectedParameterTypes != null && !AreSubstitutedParameterTypesEqual(candidate, genericTypeArguments, expectedParameterTypes))
                continue;

            yield return candidate;
        }
    }

    private static bool AreSubstitutedParameterTypesEqual(MethodInfo candidate, Type[] substitutions, Type[] expectedParameterTypes)
    {
        int expectedParameterCount = expectedParameterTypes.Length;
        ParameterInfo[] candidateParameters = candidate.GetParameters();
        if (candidateParameters.Length != expectedParameterCount)
            return false;
        for (int i = 0; i < expectedParameterCount; i++)
        {
            if (!IsSubstitutedTypeEqual(candidate: candidateParameters[i].ParameterType, expected: expectedParameterTypes[i], substitutions: substitutions))
                return false;
        }
        return true;
    }

    private static bool IsSubstitutedTypeEqual(Type candidate, Type expected, Type[] substitutions)
    {
        if (substitutions != null && candidate.IsGenericParameter && candidate.DeclaringMethod != null)
        {
            candidate = substitutions[candidate.GenericParameterPosition];
            substitutions = null; // Don't recursively replace generic parameters in the substitute type - especially important if the substitute type contains a generic parameter of the method.
        }

        if (expected.IsTypeDefinition)
        {
            return candidate.Equals(expected);
        }
        else if (expected.IsSZArray)
        {
            return candidate.IsSZArray && IsSubstitutedTypeEqual(candidate.GetElementType(), expected.GetElementType(), substitutions);
        }
        else if (expected.IsVariableBoundArray)
        {
            return candidate.IsVariableBoundArray && expected.GetArrayRank() == candidate.GetArrayRank() && IsSubstitutedTypeEqual(candidate.GetElementType(), expected.GetElementType(), substitutions);
        }
        else if (expected.IsByRef)
        {
            return candidate.IsByRef && IsSubstitutedTypeEqual(candidate.GetElementType(), expected.GetElementType(), substitutions);
        }
        else if (expected.IsPointer)
        {
            return candidate.IsPointer && IsSubstitutedTypeEqual(candidate.GetElementType(), expected.GetElementType(), substitutions);
        }
        else if (expected.IsConstructedGenericType)
        {
            if (!candidate.IsConstructedGenericType)
                return false;
            if (!candidate.GetGenericTypeDefinition().Equals(expected.GetGenericTypeDefinition()))
                return false;

            Type[] expectedGenericTypeArgs = expected.GenericTypeArguments;
            Type[] candidateGenericTypeArgs = candidate.GenericTypeArguments;
            int count = expectedGenericTypeArgs.Length;
            if (count != candidateGenericTypeArgs.Length)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (!IsSubstitutedTypeEqual(candidateGenericTypeArgs[i], expectedGenericTypeArgs[i], substitutions))
                    return false;
            }

            return true;
        }
        else if (expected.IsGenericParameter)
        {
            return candidate.Equals(expected);
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}

