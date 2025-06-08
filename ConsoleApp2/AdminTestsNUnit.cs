using NUnit.Framework;
using WorkshopManager.Models;
using Microsoft.AspNetCore.Identity;
using System;

namespace WorkshopManager.ConsoleApp2;

public class AdminTestsNUnit
{
    [Test]
    public void Admin_CanAssignMechanicToServiceOrder()
    {
        // Arrange
        var mechanic = new IdentityUser { Id = "mech-2", UserName = "mechanik2" };
        var order = new ServiceOrder();

        // Act
        order.AssignedMechanicId = mechanic.Id;
        order.AssignedMechanic = mechanic;

        // Assert
        Assert.AreEqual("mech-2", order.AssignedMechanicId);
        Assert.AreEqual("mechanik2", order.AssignedMechanic.UserName);
    }

    [Test]
    public void Admin_CanChangeServiceOrderStatus()
    {
        // Arrange
        var order = new ServiceOrder { Status = ServiceOrderStatus.Nowe };

        // Act
        order.Status = ServiceOrderStatus.WTrakcie;

        // Assert
        Assert.AreEqual(ServiceOrderStatus.WTrakcie, order.Status);
    }

    [Test]
    public void Admin_CanCloseServiceOrder()
    {
        // Arrange
        var order = new ServiceOrder { Status = ServiceOrderStatus.WTrakcie };
        var closeDate = DateTime.Now;

        // Act
        order.Status = ServiceOrderStatus.Zakonczone;
        order.ClosedAt = closeDate;

        // Assert
        Assert.AreEqual(ServiceOrderStatus.Zakonczone, order.Status);
        Assert.AreEqual(closeDate, order.ClosedAt);
    }

    [Test]
    public void Admin_CanEditProblemDescription()
    {
        // Arrange
        var order = new ServiceOrder { ProblemDescription = "Stary opis" };

        // Act
        order.ProblemDescription = "Nowy opis problemu";

        // Assert
        Assert.AreEqual("Nowy opis problemu", order.ProblemDescription);
    }
}

