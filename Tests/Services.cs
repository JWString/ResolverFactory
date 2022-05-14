using Services;

namespace Services
{
    public class CycleServiceA
    {

        IResolver<CycleServiceB> _rServiceB;

        public CycleServiceA(IResolver<CycleServiceB> rServiceB)
        {
            _rServiceB = rServiceB;
        }

        public string Value { get => "CycleServiceA"; }

        public string Test()
        {
            return _rServiceB.Resolve(s => s.Test());
        }
    }

    public class CycleServiceB
    {
        IResolver<CycleServiceC> _rServiceC;

        public CycleServiceB(IResolver<CycleServiceC> rServiceC)
        {
            _rServiceC = rServiceC;
        }

        public string Value { get => "CycleServiceB"; }

        public string Test()
        {
            return _rServiceC.Resolve(s => s.Test());
        }
    }

    public class CycleServiceC
    {
        IResolver<CycleServiceA> _rServiceA;

        public CycleServiceC(IResolver<CycleServiceA> rServiceA)
        {
            _rServiceA = rServiceA;
        }

        public string Value { get => "CycleServiceC"; }

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

        IResolver<StandardServiceB> _rServiceB;

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
            if (OnDispose != null)
            {
                OnDispose(); 
            }
        }
    }

    public class StandardServiceB : IDisposable
    {
        IResolver<StandardServiceC> _rServiceC;

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
            if (OnDispose != null)
            {
                OnDispose();
            }
        }
    }

    public class StandardServiceC : IDisposable
    {
        IResolver<StandardServiceA> _rServiceA;

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
            if (OnDispose != null)
            {
                OnDispose();
            }
        }
    }
}