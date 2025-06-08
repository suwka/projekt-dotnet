using NUnit.Framework;
using WorkshopManager.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WorkshopManager.ConsoleApp2;

public class MechanicTestsNUnit
{
    [Test]
    public void ServiceOrder_AssignedMechanic_CanBeSetAndRetrieved()
    {
        var mechanic = new IdentityUser { UserName = "mechanik1" };
        var order = new ServiceOrder
        {
            AssignedMechanicId = "mech-1",
            AssignedMechanic = mechanic
        };
        Assert.AreEqual("mech-1", order.AssignedMechanicId);
        Assert.AreEqual("mechanik1", order.AssignedMechanic.UserName);
    }

    [Test]
    public void ServiceTask_Initialization_SetsPropertiesCorrectly()
    {
        var task = new ServiceTask
        {
            Description = "Wymiana oleju",
            LaborCost = 150.00m,
            ServiceOrderId = 5
        };
        Assert.AreEqual("Wymiana oleju", task.Description);
        Assert.AreEqual(150.00m, task.LaborCost);
        Assert.AreEqual(5, task.ServiceOrderId);
    }

    [Test]
    public void ServiceOrder_CanContainMultipleServiceTasks()
    {
        var order = new ServiceOrder();
        var task1 = new ServiceTask { Description = "Wymiana klocków" };
        var task2 = new ServiceTask { Description = "Regeneracja alternatora" };
        order.ServiceTasks.Add(task1);
        order.ServiceTasks.Add(task2);
        Assert.AreEqual(2, order.ServiceTasks.Count);
        Assert.Contains(task1, (System.Collections.ICollection)order.ServiceTasks);
        Assert.Contains(task2, (System.Collections.ICollection)order.ServiceTasks);
    }
}

