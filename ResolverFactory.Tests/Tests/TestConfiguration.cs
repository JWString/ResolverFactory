﻿using TestFixtures;

namespace Tests
{
    public class StandardContainer : ResolverTests
    {
        public StandardContainer() : base(new StandardContainerFixture()) { }
    }

    public class AutofacContainer : ResolverTests
    {
        public AutofacContainer() : base(new AutofacContainerFixture()) { }
    }

}