using System;
using System.Collections.Generic;

namespace NHUnitExample.Entities
{
    public class Customer
    {
        public Customer()
        {
            Addresses = new List<Address>();
            PhoneNumbers = new List<CustomerPhone>();
        }

        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual DateTime BirthDate { get; set; }
        public virtual IList<Address> Addresses { get; set; }
        public virtual IList<CustomerPhone> PhoneNumbers { get; set; }
        public virtual CustomerCart Cart { get; set; }

        public virtual void SetCart(CustomerCart cart)
        {
            cart.Customer = this;
            Cart = cart;
        }

        public virtual void AddAddress(Address address)
        {
            address.Customer = this;
            Addresses.Add(address);
        }

        public virtual void AddPhoneNumber(CustomerPhone phone)
        {
            phone.Customer = this;
            PhoneNumbers.Add(phone);
        }
    }
}