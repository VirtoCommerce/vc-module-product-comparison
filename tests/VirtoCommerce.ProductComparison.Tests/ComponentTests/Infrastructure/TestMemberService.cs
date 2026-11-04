using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;

/// <summary>
/// The customer module, as the cleanup reads it: a user's contact (or employee) and the organizations it belongs
/// to, which are the ones the sign-in token may carry. Writes are not the module's business, so they throw.
/// </summary>
internal sealed class TestMemberService : IMemberService
{
    private readonly ConcurrentDictionary<string, Member> _members = new(StringComparer.Ordinal);

    public Contact AddContact(string memberId, params string[] organizationIds)
    {
        var contact = new Contact { Id = memberId, Name = memberId, Organizations = organizationIds.ToList() };
        _members[memberId] = contact;

        return contact;
    }

    public Employee AddEmployee(string memberId, params string[] organizationIds)
    {
        var employee = new Employee { Id = memberId, Name = memberId, Organizations = organizationIds.ToList() };
        _members[memberId] = employee;

        return employee;
    }

    public Task<Member[]> GetByIdsAsync(string[] memberIds, string responseGroup = null, string[] memberTypes = null)
    {
        var members = memberIds
            .Select(id => _members.GetValueOrDefault(id))
            .Where(x => x != null)
            .ToArray();

        return Task.FromResult(members);
    }

    public async Task<Member> GetByIdAsync(string memberId, string responseGroup = null, string memberType = null)
    {
        return (await GetByIdsAsync([memberId], responseGroup)).FirstOrDefault();
    }

    public Task SaveChangesAsync(Member[] members) => throw new NotSupportedException();

    public Task DeleteAsync(string[] ids, string[] memberTypes = null) => throw new NotSupportedException();
}
