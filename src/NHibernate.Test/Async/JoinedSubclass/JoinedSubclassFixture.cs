﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using NHibernate.Criterion;
using NUnit.Framework;

namespace NHibernate.Test.JoinedSubclass
{
	using System.Threading.Tasks;
	/// <summary>
	/// Test the use of <c>&lt;class&gt;</c> and <c>&lt;joined-subclass&gt;</c> mappings.
	/// </summary>
	[TestFixture]
	public class JoinedSubclassFixtureAsync : TestCase
	{
		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}

		protected override IList Mappings
		{
			get { return new string[] {"JoinedSubclass.JoinedSubclass.hbm.xml"}; }
		}

		[Test]
		public async Task TestJoinedSubclassAsync()
		{
			ISession s = OpenSession();
			ITransaction t = s.BeginTransaction();
			Employee mark = new Employee();
			mark.Name = "Mark";
			mark.Title = "internal sales";
			mark.Sex = 'M';
			mark.Address.Street = "buckhead";
			mark.Address.Zip = "30305";
			mark.Address.Country = "USA";

			Customer joe = new Customer();
			joe.Name = "Joe";
			joe.Address.Street = "San Francisco";
			joe.Address.Zip = "54353";
			joe.Address.Country = "USA";
			joe.Comments = "very demanding";
			joe.Sex = 'M';
			joe.Salesperson = mark;

			Person mom = new Person();
			mom.Name = "mom";
			mom.Sex = 'F';

			await (s.SaveAsync(mom));
			await (s.SaveAsync(mark));
			await (s.SaveAsync(joe));

			Assert.AreEqual(3, (await (s.CreateQuery("from Person").ListAsync())).Count);
			IQuery query = s.CreateQuery("from Customer");
			IList results = await (query.ListAsync());
			Assert.AreEqual(1, results.Count);
			Assert.IsTrue(results[0] is Customer, "should be a customer");

			s.Clear();

			IList customers = await (s.CreateQuery("from Customer c left join fetch c.Salesperson").ListAsync());
			foreach (Customer c in customers)
			{
				// when proxies is working this is important
				Assert.IsTrue(NHibernateUtil.IsInitialized(c.Salesperson));
				Assert.AreEqual("Mark", c.Salesperson.Name);
			}
			Assert.AreEqual(1, customers.Count);
			s.Clear();

			customers = await (s.CreateQuery("from Customer").ListAsync());
			foreach (Customer c in customers)
			{
				Assert.IsFalse(NHibernateUtil.IsInitialized(c.Salesperson));
				Assert.AreEqual("Mark", c.Salesperson.Name);
			}
			Assert.AreEqual(1, customers.Count);
			s.Clear();

			mark = (Employee) await (s.LoadAsync(typeof(Employee), mark.Id));
			joe = (Customer) await (s.LoadAsync(typeof(Customer), joe.Id));

			mark.Address.Zip = "30306";
			Assert.AreEqual(1, (await (s.CreateQuery("from Person p where p.Address.Zip = '30306'").ListAsync())).Count);

			await (s.DeleteAsync(mom));
			await (s.DeleteAsync(joe));
			await (s.DeleteAsync(mark));

			Assert.AreEqual(0, (await (s.CreateQuery("from Person").ListAsync())).Count);
			await (t.CommitAsync());
			s.Close();
		}

		[Test]
		public async Task TestHqlAsync()
		{
			// test the Save
			ISession s = OpenSession();
			ITransaction t = s.BeginTransaction();

			Employee wally = new Employee();
			wally.Name = "wally";
			wally.Title = "Unmanaged Employee";

			Employee dilbert = new Employee();
			dilbert.Name = "dilbert";
			dilbert.Title = "office clown";

			Employee pointyhair = new Employee();
			pointyhair.Name = "pointyhair";
			pointyhair.Title = "clown watcher";

			dilbert.Manager = pointyhair;

			await (s.SaveAsync(wally));
			await (s.SaveAsync(dilbert));
			await (s.SaveAsync(pointyhair));
			await (t.CommitAsync());
			s.Close();

			// get a proxied - initialized version of manager
			s = OpenSession();
			pointyhair = (Employee) await (s.LoadAsync(typeof(Employee), pointyhair.Id));
			await (NHibernateUtil.InitializeAsync(pointyhair));
			s.Close();

			s = OpenSession();
			t = s.BeginTransaction();
			IQuery q = s.CreateQuery("from Employee as e where e.Manager = :theMgr");
			q.SetParameter("theMgr", pointyhair);
			IList results = await (q.ListAsync());
			Assert.AreEqual(1, results.Count, "should only return 1 employee.");
			dilbert = (Employee) results[0];
			Assert.AreEqual("dilbert", dilbert.Name, "should have been dilbert returned.");

			await (s.DeleteAsync(wally));
			await (s.DeleteAsync(pointyhair));
			await (s.DeleteAsync(dilbert));
			await (s.FlushAsync());
			await (t.CommitAsync());
			s.Close();
		}

		[Test]
		public async Task SelectByClassAsync()
		{
			using (ISession s = OpenSession())
			using (ITransaction t = s.BeginTransaction())
			{
				Person wally = new Person();
				wally.Name = "wally";

				Employee dilbert = new Employee();
				dilbert.Name = "dilbert";
				dilbert.Title = "office clown";

				await (s.SaveAsync(wally));
				await (s.SaveAsync(dilbert));
				await (s.FlushAsync());

				Assert.AreSame(wally, await (s.CreateQuery("select p from Person p where p.class = Person").UniqueResultAsync()));
				Assert.AreSame(dilbert, await (s.CreateQuery("select p from Person p where p.class = Employee").UniqueResultAsync()));

				await (s.DeleteAsync(wally));
				await (s.DeleteAsync(dilbert));
				await (t.CommitAsync());
			}
		}

		/// <summary>
		/// Test the ability to insert a new row with a User Assigned Key
		/// Right now - the only way to verify this is to watch SQL Profiler
		/// </summary>
		[Test]
		public async Task TestCRUDAsync()
		{
			// test the Save
			ISession s = OpenSession();
			ITransaction t = s.BeginTransaction();

			Employee emp = new Employee();
			emp.Name = "test one";
			emp.Title = "office clown";

			await (s.SaveAsync(emp));

			Person person = new Person();
			person.Name = "the test string";

			await (s.SaveAsync(person));

			await (t.CommitAsync());
			s.Close();

			int empId = emp.Id;
			int personId = person.Id;

			// lets verify the correct classes were saved
			s = OpenSession();
			t = s.BeginTransaction();

			// perform a load based on the base class
			Person empAsPerson = (Person) (await (s.CreateQuery("from Person as p where p.id = ?").SetInt32(0, empId).ListAsync()))[0];
			person = (Person) await (s.LoadAsync(typeof(Person), personId));

			// the object with id=2 was loaded using the base class - lets make sure it actually loaded
			// the subclass
			Assert.AreEqual(typeof(Employee), empAsPerson.GetType(),
			                "even though person was queried, should have returned correct subclass.");
			emp = (Employee) await (s.LoadAsync(typeof(Employee), empId));

			// lets update the objects
			person.Name = "Did it get updated";
			person.Sex = 'M';

			// update the properties from the subclass and base class
			emp.Name = "Updated Employee String";
			emp.Title = "office dunce";

			// verify the it actually changes the same object
			Assert.AreEqual(emp.Name, empAsPerson.Name, "emp and empAsPerson should refer to same object.");

			// save it through the base class reference and make sure that the
			// subclass properties get updated.
			await (s.UpdateAsync(empAsPerson));
			await (s.UpdateAsync(person));

			await (t.CommitAsync());
			s.Close();

			// lets test the Criteria interface for subclassing
			s = OpenSession();
			t = s.BeginTransaction();

			IList results = await (s.CreateCriteria(typeof(Person))
				.Add(Expression.In("Name", new string[] {"Did it get updated", "Updated Employee String"}))
				.ListAsync());

			Assert.AreEqual(2, results.Count);

			person = null;
			emp = null;

			foreach (Person obj in results)
			{
				if (obj is Employee)
					emp = (Employee) obj;
				else
					person = obj;
			}

			// verify the properties got updated
			Assert.AreEqual('M', person.Sex);
			Assert.AreEqual("office dunce", emp.Title);

			await (s.DeleteAsync(emp));
			await (s.DeleteAsync(person));

			await (t.CommitAsync());
			s.Close();
		}
	}
}
