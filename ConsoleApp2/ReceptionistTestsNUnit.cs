using NUnit.Framework;
using WorkshopManager.Models;
using System;

namespace WorkshopManager.ConsoleApp2;

public class ReceptionistTestsNUnit
{
    [Test]
    public void Receptionist_CanCreateServiceOrder()
    {
        var order = new ServiceOrder
        {
            Status = ServiceOrderStatus.Nowe,
            CreatedAt = DateTime.Now,
            ProblemDescription = "Brak świateł przednich",
            VehicleId = 1
        };
        Assert.AreEqual(ServiceOrderStatus.Nowe, order.Status);
        Assert.AreEqual("Brak świateł przednich", order.ProblemDescription);
        Assert.AreEqual(1, order.VehicleId);
        Assert.That(order.CreatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void Receptionist_CanEditProblemDescription()
    {
        var order = new ServiceOrder { ProblemDescription = "Stary opis" };
        order.ProblemDescription = "Nowy opis od recepcji";
        Assert.AreEqual("Nowy opis od recepcji", order.ProblemDescription);
    }
}

