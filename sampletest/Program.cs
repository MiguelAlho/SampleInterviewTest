using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Customer Customer = new Customer() { Id = 1, Name = "John Doe", Address = "Cool Streem 10, London N13LR" };
            Product Product1 = new Product() { Id = 1, Name = "Awesome TV", Price = 10.50M };
            Product Product2 = new Product() { Id = 2, Name = "Awesome PC", Price = 150.99M };

            OrderRepository repository = new OrderRepository();
            OrderIdGenerator generator = new OrderIdGenerator();

            //This simulates an API controller, reduced to a class to simplify test scope
            var checkout = new CheckoutController(repository, generator);
            //this simulates incoming API calls (POST / PUTS)
            var orderId = checkout.CreateOrder(new CreateOrderCommand { Customer = Customer.Id, Cart = new List<Product> { Product1, Product2 } });
            checkout.ApplyDiscount(new ApplyDiscountCommand { OrderId = orderId, DiscountCode = "BlackFridaySpecial" });
            checkout.PlaceOrder(new PlaceOrderCommand { OrderId = orderId , OrderStatus = (int)OrderStatus.Ordered});
             
            // The following is just output to follow along what is being done in the sample
            var order = repository.GetOrder(orderId);
            Console.WriteLine($"Order info: order {order.Id} for customer {order.Customer}");
            Console.WriteLine($"\t{order.Cart.Count} products");
            foreach (Product p in order.Cart)
            {
                Console.WriteLine($"\t\t{p.Name} {p.Price}");
            }
            Console.WriteLine($"\t{order.Cart.Count} products");
            Console.WriteLine($"\t{order.DiscountCodeApplied} discount code applied: {order.Discount * 100}%");
            Console.WriteLine($"\tShipping : {order.Shipping} ");
            Console.WriteLine($"\tTotal: {order.Total}");

            Console.WriteLine("[Enter] to exit...");
            Console.ReadLine();
        }
    }


    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public enum OrderStatus
    {
        Composing,
        Ordered,
        Paid,
        Delivered,
        Canceled
    }

    public class Order
    {
        public int Id { get; set; }
        public int Customer { get; set; }
        public List<Product> Cart { get; set; }
        public string DiscountCodeApplied { get; set; }
        public decimal Discount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }
    }

    public class CreateOrderCommand
    {
        public int Customer { get; set; }
        public List<Product> Cart { get; set; }
    }

    public class ApplyDiscountCommand
    {
        public int OrderId { get; set; }
        public string DiscountCode { get; set; }
    }

    public class PlaceOrderCommand
    {
        public int OrderId { get; set; }
        public int OrderStatus { get; set; }
    }

    public class OrderQuery
    {
        public int OrderId { get; set; }
    }

    public class CheckoutController
    {
        public OrderRepository Repository { get; set; }
        public IOrderIdGenerator IdGenerator { get; set; }
        
        public CheckoutController(OrderRepository repository, IOrderIdGenerator idGenerator)
        {
            Repository = repository;
            IdGenerator = idGenerator;
        }

        public int CreateOrder(CreateOrderCommand command)
        {
            var order = new Order()
            {
                Id = IdGenerator.GetNewOrderId(),
                Customer = command.Customer,
                Cart = command.Cart.ToList(),
                DiscountCodeApplied = null,
                Discount = 0.0M,
                Status = OrderStatus.Composing
            };

            for (int i = 0; i < order.Cart.Count; i++)
            {
                order.Total += order.Cart[i].Price;
            }

            if (order.Total > 50)
                order.Shipping = 0.0M;
            else
                order.Shipping = 10.0M;

            order.Total += order.Shipping;

            Repository.SaveOrder(order);

            return order.Id;
        }

        public void ApplyDiscount(ApplyDiscountCommand command)
        {
            var order = Repository.GetOrder(command.OrderId);

            order.DiscountCodeApplied = command.DiscountCode;

            if (string.IsNullOrEmpty(order.DiscountCodeApplied))
                order.Discount = 0;
            else if (order.DiscountCodeApplied == "BlackFridaySpecial")
            {
                order.Discount = 0.10M;
                order.Shipping = 0.0M;
            }
            else if (order.DiscountCodeApplied.StartsWith("Currys"))
                order.Discount = 0.05M;
            else if (order.DiscountCodeApplied == "Campaign3D")
            {
                order.Discount = 0.12M;
                order.Shipping = 5.0M;
            }
            else
                order.Discount = 0;

            order.Total = 0.0M;
            for (int i = 0; i < order.Cart.Count; i++)
            {
                order.Total += order.Cart[i].Price;
            }

            order.Total = order.Total - (order.Total * order.Discount);
            order.Total += order.Shipping;

            Repository.SaveOrder(order);
        }

        public void PlaceOrder(PlaceOrderCommand command)
        {
            var order = Repository.GetOrder(command.OrderId);

            order.Status = OrderStatus.Ordered;

            Repository.SaveOrder(order);
        }

        public Order GetOrder(OrderQuery request)
        {
            return Repository.GetOrder(request.OrderId);
        }
    }

    public class OrderRepository
    {
        readonly Dictionary<int, Order> _orders = new Dictionary<int, Order>();

        public void SaveOrder(Order order)
        {
            if (!_orders.ContainsKey(order.Id))
                _orders.Add(order.Id, order);
            else
                _orders[order.Id] = order;
        }

        public Order GetOrder(int id)
        {
            if (_orders.ContainsKey(id))
                return _orders[id];
            else
                return null;
        }
    }

    public interface IOrderIdGenerator
    {
        int GetNewOrderId();
    }

    public class OrderIdGenerator : IOrderIdGenerator
    {
        int lastId = 0;

        public int GetNewOrderId()
        {
            return ++lastId;
        }
    }

}
