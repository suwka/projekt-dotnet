using NUnit.Framework;
using WorkshopManager.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WorkshopManager.ConsoleApp2;

public class CustomerTestsNUnit
{
    [Test]
    public void Customer_Initialization_SetsPropertiesCorrectly()
    {
        var customer = new Customer
        {
            FirstName = "Anna",
            LastName = "Nowak",
            Phone = "987654321",
            IdentityUserId = "user-2"
        };
        Assert.AreEqual("Anna", customer.FirstName);
        Assert.AreEqual("Nowak", customer.LastName);
        Assert.AreEqual("987654321", customer.Phone);
        Assert.AreEqual("user-2", customer.IdentityUserId);
    }

    [Test]
    public void Customer_CanAssignVehiclesCollection()
    {
        var customer = new Customer();
        var vehicles = new List<Vehicle>();
        customer.Vehicles = vehicles;
        Assert.AreSame(vehicles, customer.Vehicles);
    }

    [Test]
    public void Customer_IdentityUser_CanBeAssignedAndRetrieved()
    {
        var customer = new Customer();
        var identityUser = new IdentityUser { UserName = "testuser" };
        customer.IdentityUser = identityUser;
        Assert.AreEqual("testuser", customer.IdentityUser.UserName);
    }
}

