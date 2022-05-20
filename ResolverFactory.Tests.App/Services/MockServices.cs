using ResolverFactory;

namespace Services
{
    public class CycleServiceA
    {
        readonly IResolver<CycleServiceB> _rServiceB;

        public CycleServiceA(IResolver<CycleServiceB> rServiceB)
        {
            _rServiceB = rServiceB;
        }

        public string Test()
        {
            return _rServiceB.Resolve(s => s.Test());
        }
    }

    public class CycleServiceB
    {
        readonly IResolver<CycleServiceC> _rServiceC;

        public CycleServiceB(IResolver<CycleServiceC> rServiceC)
        {
            _rServiceC = rServiceC;
        }

        public string Test()
        {
            return _rServiceC.Resolve(s => s.Test());
        }
    }

    public class CycleServiceC
    {
        readonly IResolver<CycleServiceA> _rServiceA;

        public CycleServiceC(IResolver<CycleServiceA> rServiceA)
        {
            _rServiceA = rServiceA;
        }

        public string Test()
        {
            return _rServiceA.Resolve(s => s.Test());
        }
    }

    public class StandardService
    {
        public string Value { get => "StandardService"; }
    }

    public class StandardServiceA : IDisposable
    {
        readonly IResolver<StandardServiceB> _rServiceB;

        public StandardServiceA(IResolver<StandardServiceB> rServiceB)
        {
            _rServiceB = rServiceB;
        }

        public string Value { get => "StandardServiceA"; }

        public string Test()
        {
            return _rServiceB.Resolve(s => s.Value);
        }

        public Action? OnDispose { get; set; }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }

    public class StandardServiceB : IDisposable
    {
        readonly IResolver<StandardServiceC> _rServiceC;

        public StandardServiceB(IResolver<StandardServiceC> rServiceC)
        {
            _rServiceC = rServiceC;
        }

        public string Value { get => "StandardServiceB"; }

        public string Test()
        {
            return _rServiceC.Resolve(s => s.Value);
        }

        public Action? OnDispose { get; set; }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }

    public class StandardServiceC : IDisposable
    {
        readonly IResolver<StandardServiceA> _rServiceA;

        public StandardServiceC(IResolver<StandardServiceA> rServiceA)
        {
            _rServiceA = rServiceA;
        }

        public string Value { get => "StandardServiceC"; }

        public string Test()
        {
            return _rServiceA.Resolve(s => s.Value);
        }

        public Action? OnDispose { get; set; }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}