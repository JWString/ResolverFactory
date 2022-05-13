using ResolverFactory;

namespace Services
{
    public class ServiceA
    {

        IResolverFactory<ServiceB> _rServiceB;

        public ServiceA(IResolverFactory<ServiceB> rServiceB)
        {
            _rServiceB = rServiceB;
        }

        public string Value { get => "ServiceA"; }

        public string Test()
        {
            //return _rServiceB.CreateResolver(s => s.Value)();
            return _rServiceB.CreateResolver(s => s.Test())();
        }
    }

    public class ServiceB
    {
        IResolverFactory<ServiceC> _rServiceC;

        public ServiceB(IResolverFactory<ServiceC> rServiceC)
        {
            _rServiceC = rServiceC;
        }

        public string Value { get => "ServiceB"; }

        public string Test()
        {
            //return _rServiceC.CreateResolver(s => s.Value)();
            return _rServiceC.CreateResolver(s => s.Test())();
        }
    }

    public class ServiceC
    {
        IResolverFactory<ServiceA> _rServiceA;

        public ServiceC(IResolverFactory<ServiceA> rServiceA)
        {
            _rServiceA = rServiceA;
        }

        public string Value { get => "ServiceC"; }

        public string Test()
        {
            //return _rServiceA.CreateResolver(s => s.Value)();
            return _rServiceA.CreateResolver(s => s.Test())();
        }
    }

    public class NonCircular
    {
        public string Value { get => "Noncircular"; }
    }

    public class StandardServiceA
    {

        StandardServiceB _serviceB;

        public StandardServiceA(StandardServiceB serviceB)
        {
            _serviceB = serviceB;
        }

        public string Value { get => "ServiceA"; }

        public string Test()
        {
            return _serviceB.Value;
        }
    }

    public class StandardServiceB
    {
        StandardServiceC _serviceC;

        public StandardServiceB(StandardServiceC serviceC)
        {
            _serviceC = serviceC;
        }

        public string Value { get => "ServiceB"; }

        public string Test()
        {
            return _serviceC.Value;
        }
    }

    public class StandardServiceC
    {
        StandardServiceA _serviceA;

        public StandardServiceC(StandardServiceA serviceA)
        {
            _serviceA = serviceA;
        }

        public string Value { get => "ServiceC"; }

        public string Test()
        {
            return _serviceA.Value;
        }
    }
}