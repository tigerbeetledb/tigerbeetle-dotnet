using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TigerBeetle.Tests
{
	[TestClass]
	public class AccountTests
	{
		#region Fields

		private static Client GetClient() => new Client(0, new IPEndPoint[] { IPEndPoint.Parse($"127.0.0.1:{TBServer.TB_PORT}") }, maxConcurrency: 100);

		private static readonly Account[] accounts = new[]
		{
			new Account
			{
				Id = Guid.NewGuid(),
				Ledger = 1,
				Code = 1,
			},
			new Account
			{
				Id = Guid.NewGuid(),
				Ledger = 1,
				Code = 2,
			}
		};

		#endregion Fields

		[TestMethod]
		[DoNotParallelize]
		public void CreateAccounts()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = client.CreateAccounts(accounts);
				Assert.IsTrue(results.Length == 0);

				var lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);
			}
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task CreateAccountsAsync()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = await client.CreateAccountsAsync(accounts);
				Assert.IsTrue(results.Length == 0);

				var lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);
			}
		}

		private void AssertAccounts(Account[] lookupAccounts)
		{
			Assert.IsTrue(lookupAccounts.Length == 2);
			Assert.AreEqual(lookupAccounts[0].Id, accounts[0].Id);
			Assert.AreEqual(lookupAccounts[0].Code, accounts[0].Code);
			Assert.AreEqual(lookupAccounts[0].Ledger, accounts[0].Ledger);

			Assert.AreEqual(lookupAccounts[1].Id, accounts[1].Id);
			Assert.AreEqual(lookupAccounts[1].Code, accounts[1].Code);
			Assert.AreEqual(lookupAccounts[1].Ledger, accounts[1].Ledger);
		}

		[TestMethod]
		[DoNotParallelize]
		public void CreateTransfers()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = client.CreateAccounts(accounts);
				Assert.IsTrue(results.Length == 0);

				var transfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
				};

				var result = client.CreateTransfer(transfer);
				Assert.IsTrue(result == CreateTransferResult.Ok);

				var lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, transfer.Amount);
			}
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task CreateTransfersAsync()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = await client.CreateAccountsAsync(accounts);
				Assert.IsTrue(results.Length == 0);

				var transfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
				};

				var result = await client.CreateTransferAsync(transfer);
				Assert.IsTrue(result == CreateTransferResult.Ok);

				var lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, transfer.Amount);
			}
		}


		[TestMethod]
		[DoNotParallelize]
		public void CreatePendingTransfers()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = client.CreateAccounts(accounts);
				Assert.IsTrue(results.Length == 0);

				var transfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
					Flags = TransferFlags.Pending,
					Timeout = int.MaxValue,
				};

				var result = client.CreateTransfer(transfer);
				Assert.IsTrue(result == CreateTransferResult.Ok);

				var lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPending, transfer.Amount);
				Assert.AreEqual(lookupAccounts[0].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPending, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, 0u);

				var postTransfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
					Flags = TransferFlags.PostPendingTransfer,
					PendingId = transfer.Id,
				};

				var postResult = client.CreateTransfer(postTransfer);
				Assert.IsTrue(postResult == CreateTransferResult.Ok);

				lookupAccounts = client.LookupAccounts(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, 0u);
			}
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task CreatePendingTransfersAsync()
		{
			using (var server = new TBServer())
			{
				using var client = GetClient();

				var results = await client.CreateAccountsAsync(accounts);
				Assert.IsTrue(results.Length == 0);

				var transfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
					Flags = TransferFlags.Pending,
					Timeout = int.MaxValue,
				};

				var result = await client.CreateTransferAsync(transfer);
				Assert.IsTrue(result == CreateTransferResult.Ok);

				var lookupAccounts = await client.LookupAccountsAsync(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPending, transfer.Amount);
				Assert.AreEqual(lookupAccounts[0].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPending, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, 0u);

				var postTransfer = new Transfer
				{
					Id = Guid.NewGuid(),
					CreditAccountId = accounts[0].Id,
					DebitAccountId = accounts[1].Id,
					Ledger = 1,
					Code = 1,
					Amount = 100,
					Flags = TransferFlags.PostPendingTransfer,
					PendingId = transfer.Id,
				};

				var postResult = await client.CreateTransferAsync(postTransfer);
				Assert.IsTrue(postResult == CreateTransferResult.Ok);

				lookupAccounts = await client.LookupAccountsAsync(new[] { accounts[0].Id, accounts[1].Id });
				AssertAccounts(lookupAccounts);

				Assert.AreEqual(lookupAccounts[0].CreditsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[0].DebitsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, 0u);

				Assert.AreEqual(lookupAccounts[1].CreditsPosted, 0u);
				Assert.AreEqual(lookupAccounts[1].CreditsPending, 0u);
				Assert.AreEqual(lookupAccounts[1].DebitsPosted, transfer.Amount);
				Assert.AreEqual(lookupAccounts[1].DebitsPending, 0u);
			}
		}

	}

	internal class TBServer : IDisposable
	{
		#region Fields

		public const string TB_PATH = "../../../../../../tigerbeetle/";
		public static readonly string TB_SERVER = $"{TB_PATH}/tigerbeetle";
		public const int TB_PORT = 3001;
		public const string TB_FILE = "dotnet-tests.tigerbeetle";
		public static readonly string FORMAT = $"format --cluster=0 --replica=0 ./{TB_FILE}";
		public static readonly string START = $"start --addresses={TB_PORT} ./{TB_FILE}";

		private Process process;

		#endregion Fields

		#region Constructor

		public TBServer()
		{
			var format = Process.Start(TB_SERVER, FORMAT);
			format.WaitForExit();

			process = Process.Start(TB_SERVER, START);
		}

		#endregion Constructor

		#region Methods

		public void Dispose()
		{
			process.Kill();
			process.Dispose();

			File.Delete($"{TB_PATH}{TB_FILE}");
		}

		#endregion Methods
	}
}