
namespace UsingNamespaceStatementAnalyzer.Account
{
    class Id
    {
        public string GetValue() => "IdValue";
    }
}

namespace UsingNamespaceStatementAnalyzer.Transaction
{
    class Current
    {
        private readonly Account.Id _accountId;

        public Current(Account.Id accountId)
        {
            _accountId = accountId;
        }

        public string GetFancyValue() => _accountId.GetValue() + " - FancyValue";
    }
}
