using FluentAssertions;
using NSubstitute;
using SampleTest;
using System;
using System.Collections.Generic;
using Xunit;

namespace SampleTestTests
{
    public class ProgramTests
    {

    }

    public static class MockData
    {
        public static Customer Customer { get; } = new Customer() { Id = 1, Name = "John Doe", Address = "Cool Streem 10, London N13LR" };
        public static Product Product1 { get; } = new Product() { Id = 1, Name = "Awesome TV", Price = 10.50M };
        public static Product Product2 { get; } = new Product() { Id = 2, Name = "Awesome PC", Price = 150.99M };
    }

    public class CheckoutTests
    { 
        [Fact]
        public void CanCreateOrderWithFreeShipping()
        {
            var repo = new OrderRepository();
            var generator = Substitute.For<IOrderIdGenerator>();
            generator.GetNewOrderId().Returns(1);

            var controller = new CheckoutController(repo, generator);
            var orderId = controller.CreateOrder(new CreateOrderCommand
            {
                Customer = MockData.Customer.Id,
                Cart = new List<Product> { MockData.Product1, MockData.Product2 }
            });

            Order savedOrder = repo.GetOrder(orderId);
            savedOrder.Id.Should().Be(1);
            savedOrder.Customer.Should().Be(1);
            savedOrder.Cart.Should().HaveCount(2);
            savedOrder.Status.Should().Be(OrderStatus.Composing);
            savedOrder.DiscountCodeApplied.Should().Be(null); 
            savedOrder.Discount.Should().Be(0.0M);
            savedOrder.Shipping.Should().Be(0);
            savedOrder.Total.Should().Be(161.49M);
        }

        [Fact]
        public void CanCreateOrderWithFlatRateShipping()
        {
            var repo = new OrderRepository();
            var generator = Substitute.For<IOrderIdGenerator>();
            generator.GetNewOrderId().Returns(1);

            var controller = new CheckoutController(repo, generator);
            var orderId = controller.CreateOrder(new CreateOrderCommand
            {
                Customer = MockData.Customer.Id,
                Cart = new List<Product> { MockData.Product1 }
            });

            Order savedOrder = repo.GetOrder(orderId);
            savedOrder.Id.Should().Be(1);
            savedOrder.Customer.Should().Be(1);
            savedOrder.Cart.Should().HaveCount(1);
            savedOrder.Status.Should().Be(OrderStatus.Composing);
            savedOrder.DiscountCodeApplied.Should().Be(null);
            savedOrder.Discount.Should().Be(0.0M);
            savedOrder.Shipping.Should().Be(10);
            savedOrder.Total.Should().Be(20.50M);
        }

        [Fact]
        public void CanCreateCheckoutManagerWithBlackFridayDiscountAppliedAndShippingReduced()
        {
            var repo = new OrderRepository();
            var generator = Substitute.For<IOrderIdGenerator>();
            generator.GetNewOrderId().Returns(1);

            var controller = new CheckoutController(repo, generator);
            var orderId = controller.CreateOrder(new CreateOrderCommand { 
                Customer = MockData.Customer.Id, 
                Cart = new List<Product> { MockData.Product1 } 
            });

            controller.ApplyDiscount(new ApplyDiscountCommand { OrderId = orderId, DiscountCode = "BlackFridaySpecial" });
            //applying discount twice should have no effect
            controller.ApplyDiscount(new ApplyDiscountCommand { OrderId = orderId, DiscountCode = "BlackFridaySpecial" });

            Order savedOrder = repo.GetOrder(1);
            savedOrder.Id.Should().Be(1);
            savedOrder.Customer.Should().Be(1);
            savedOrder.Cart.Should().HaveCount(1);
            savedOrder.Status.Should().Be(OrderStatus.Composing);
            savedOrder.DiscountCodeApplied.Should().Be("BlackFridaySpecial");
            savedOrder.Discount.Should().Be(0.10M);
            savedOrder.Shipping.Should().Be(0);
            savedOrder.Total.Should().Be(9.45M);
        }

        [Fact]
        public void ChangingDiscountResetsDiscountOnTotal()
        {
            var repo = new OrderRepository();
            var generator = Substitute.For<IOrderIdGenerator>();
            generator.GetNewOrderId().Returns(1);

            var controller = new CheckoutController(repo, generator);
            var orderId = controller.CreateOrder(new CreateOrderCommand
            {
                Customer = MockData.Customer.Id,
                Cart = new List<Product> { MockData.Product1}
            });

            controller.ApplyDiscount(new ApplyDiscountCommand { OrderId = orderId, DiscountCode = "BlackFridaySpecial" });
            controller.ApplyDiscount(new ApplyDiscountCommand { OrderId = orderId, DiscountCode = "Campaign3D" });
            //applying discount twice should have no effect
            
            Order savedOrder = repo.GetOrder(1);
            savedOrder.Id.Should().Be(1);
            savedOrder.Customer.Should().Be(1);
            savedOrder.Cart.Should().HaveCount(1);
            savedOrder.Status.Should().Be(OrderStatus.Composing);
            savedOrder.DiscountCodeApplied.Should().Be("Campaign3D");
            savedOrder.Discount.Should().Be(0.12M);
            savedOrder.Shipping.Should().Be(5);
            savedOrder.Total.Should().Be(14.24M);
        }
    }
}
