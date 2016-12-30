using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Dasher.Contracts.Utils;

namespace Dasher.Contracts.Types
{
    internal sealed class EnumContract : ByRefContract, IWriteContract, IReadContract
    {
        public static bool CanProcess(Type type) => type.GetTypeInfo().IsEnum;

        private HashSet<string> MemberNames { get; }

        public EnumContract(Type type)
        {
            if (!CanProcess(type))
                throw new ArgumentException("Must be an enum.", nameof(type));
            MemberNames = new HashSet<string>(Enum.GetNames(type), StringComparer.OrdinalIgnoreCase);
        }

        public EnumContract(XContainer element)
        {
            MemberNames = new HashSet<string>(element.Elements("Member").Select(e => e.Attribute("Name").Value));
        }

        public bool CanReadFrom(IWriteContract writeContract, bool strict)
        {
            var that = writeContract as EnumContract;
            if (that == null)
                return false;
            return strict
                ? MemberNames.SetEquals(that.MemberNames)
                : MemberNames.IsSupersetOf(that.MemberNames);
        }

        internal override IEnumerable<Contract> Children => EmptyArray<Contract>.Instance;

        public override bool Equals(Contract other)
        {
            var e = other as EnumContract;
            return e != null && MemberNames.SetEquals(e.MemberNames);
        }

        protected override int ComputeHashCode()
        {
            unchecked
            {
                var hash = 0;
                foreach (var memberName in MemberNames)
                {
                    hash <<= 5;
                    hash ^= memberName.GetHashCode();
                }
                return hash;
            }
        }

        internal override XElement ToXml()
        {
            if (Id == null)
                throw new InvalidOperationException("\"Id\" property cannot be null.");
            return new XElement("Enum",
                new XAttribute("Id", Id),
                MemberNames.Select(m => new XElement("Member", new XAttribute("Name", m))));
        }

        IReadContract IReadContract.CopyTo(ContractCollection collection) => collection.Intern(this);
        IWriteContract IWriteContract.CopyTo(ContractCollection collection) => collection.Intern(this);
    }
}