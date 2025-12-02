using System;
using System.Collections.Generic;
using System.Linq;

namespace TicketVendingMachine
{
    // Перечисление состояний автомата
    public enum VendingMachineState
    {
        Idle,
        WaitingForMoney,
        PartialMoneyReceived,
        MoneyReceived,
        TicketDispensing,
        TicketDispensed,
        ChangeDispensing,
        ChangeDispensed,
        TransactionCanceled,
        Error,
        MaintenanceMode,
        RefundProcessing
    }

    // Перечисление типов билетов
    public enum TicketType
    {
        Adult,
        Child,
        Student,
        Senior,
        VIP
    }

    // Класс билета
    public class Ticket
    {
        public TicketType Type { get; set; }
        public decimal Price { get; set; }
        public string Destination { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string TicketNumber { get; set; }
        
        public Ticket(TicketType type, decimal price, string destination)
        {
            Type = type;
            Price = price;
            Destination = destination;
            TicketNumber = GenerateTicketNumber();
            ValidFrom = DateTime.Now;
            ValidTo = DateTime.Now.AddHours(2);
        }
        
        private string GenerateTicketNumber()
        {
            return $"TICK-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
        
        public override string ToString()
        {
            return $"{Type} билет в {Destination} - {Price:C} (№{TicketNumber})";
        }
    }

    // Паттерн State - интерфейс состояния
    public interface IVendingMachineState
    {
        void SelectTicket(TicketType ticketType, string destination);
        void InsertMoney(decimal amount);
        void CancelTransaction();
        void DispenseTicket();
        void DispenseChange();
        void ProcessRefund();
        void EnterMaintenanceMode();
        void ExitMaintenanceMode();
        bool CanSelectTicket();
        bool CanInsertMoney();
        bool CanCancelTransaction();
        bool CanDispenseTicket();
        bool CanDispenseChange();
        string GetStatusMessage();
    }

    // Конкретные состояния

    public class IdleState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination)
        {
            _context.SelectedTicket = _context.GetTicketInfo(ticketType, destination);
            _context.ChangeState(new WaitingForMoneyState());
            Console.WriteLine($"Выбран билет: {_context.SelectedTicket}");
        }
        
        public void InsertMoney(decimal amount) => 
            Console.WriteLine("Сначала выберите билет");
        
        public void CancelTransaction() => 
            Console.WriteLine("Нет активной транзакции для отмены");
        
        public void DispenseTicket() => 
            Console.WriteLine("Нет выбранного билета");
        
        public void DispenseChange() => 
            Console.WriteLine("Нет транзакции");
        
        public void ProcessRefund() => 
            Console.WriteLine("Нет активной транзакции");
        
        public void EnterMaintenanceMode()
        {
            _context.ChangeState(new MaintenanceModeState());
            Console.WriteLine("Включен режим обслуживания");
        }
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Уже в обычном режиме");
        
        public bool CanSelectTicket() => true;
        public bool CanInsertMoney() => false;
        public bool CanCancelTransaction() => false;
        public bool CanDispenseTicket() => false;
        public bool CanDispenseChange() => false;
        
        public string GetStatusMessage() => "Готов к работе. Выберите билет.";
    }

    public class WaitingForMoneyState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination) => 
            Console.WriteLine("Уже выбран билет. Сначала завершите текущую транзакцию.");
        
        public void InsertMoney(decimal amount)
        {
            _context.InsertedMoney += amount;
            Console.WriteLine($"Внесено: {amount:C}. Всего: {_context.InsertedMoney:C}");
            
            if (_context.InsertedMoney >= _context.SelectedTicket.Price)
            {
                if (_context.InsertedMoney > _context.SelectedTicket.Price)
                {
                    _context.ChangeState(new MoneyReceivedState());
                    Console.WriteLine("Сумма получена. Сдача будет возвращена.");
                }
                else
                {
                    _context.ChangeState(new MoneyReceivedState());
                    Console.WriteLine("Точная сумма получена.");
                }
            }
            else
            {
                _context.ChangeState(new PartialMoneyReceivedState());
                Console.WriteLine($"Недостаточно средств. Нужно еще: {_context.SelectedTicket.Price - _context.InsertedMoney:C}");
            }
        }
        
        public void CancelTransaction()
        {
            _context.ChangeState(new TransactionCanceledState());
            Console.WriteLine("Транзакция отменена. Возврат денег...");
        }
        
        public void DispenseTicket() => 
            Console.WriteLine("Внесите полную сумму");
        
        public void DispenseChange() => 
            Console.WriteLine("Нет сдачи для выдачи");
        
        public void ProcessRefund() => 
            Console.WriteLine("Нет необходимости в возврате");
        
        public void EnterMaintenanceMode() => 
            Console.WriteLine("Завершите текущую транзакцию");
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Не в режиме обслуживания");
        
        public bool CanSelectTicket() => false;
        public bool CanInsertMoney() => true;
        public bool CanCancelTransaction() => true;
        public bool CanDispenseTicket() => false;
        public bool CanDispenseChange() => false;
        
        public string GetStatusMessage() => $"Ожидание оплаты. Внесено: {_context.InsertedMoney:C}, нужно: {_context.SelectedTicket.Price:C}";
    }

    public class PartialMoneyReceivedState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination) => 
            Console.WriteLine("Уже выбран билет");
        
        public void InsertMoney(decimal amount)
        {
            _context.InsertedMoney += amount;
            Console.WriteLine($"Внесено: {amount:C}. Всего: {_context.InsertedMoney:C}");
            
            if (_context.InsertedMoney >= _context.SelectedTicket.Price)
            {
                _context.ChangeState(new MoneyReceivedState());
                Console.WriteLine("Полная сумма получена");
            }
        }
        
        public void CancelTransaction()
        {
            _context.ChangeState(new TransactionCanceledState());
            Console.WriteLine("Транзакция отменена. Возврат денег...");
        }
        
        public void DispenseTicket() => 
            Console.WriteLine("Внесите полную сумму");
        
        public void DispenseChange() => 
            Console.WriteLine("Нет сдачи для выдачи");
        
        public void ProcessRefund() => 
            Console.WriteLine("Нет необходимости в возврате");
        
        public void EnterMaintenanceMode() => 
            Console.WriteLine("Завершите текущую транзакцию");
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Не в режиме обслуживания");
        
        public bool CanSelectTicket() => false;
        public bool CanInsertMoney() => true;
        public bool CanCancelTransaction() => true;
        public bool CanDispenseTicket() => false;
        public bool CanDispenseChange() => false;
        
        public string GetStatusMessage() => $"Частичная оплата. Внесено: {_context.InsertedMoney:C}, нужно еще: {_context.SelectedTicket.Price - _context.InsertedMoney:C}";
    }

    public class MoneyReceivedState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination) => 
            Console.WriteLine("Завершите текущую транзакцию");
        
        public void InsertMoney(decimal amount) => 
            Console.WriteLine("Достаточно средств. Получите билет");
        
        public void CancelTransaction()
        {
            _context.ChangeState(new TransactionCanceledState());
            Console.WriteLine("Транзакция отменена. Возврат денег...");
        }
        
        public void DispenseTicket()
        {
            _context.ChangeState(new TicketDispensingState());
            Console.WriteLine("Выдача билета...");
        }
        
        public void DispenseChange()
        {
            if (_context.InsertedMoney > _context.SelectedTicket.Price)
            {
                _context.ChangeState(new ChangeDispensingState());
                Console.WriteLine("Выдача сдачи...");
            }
            else
            {
                Console.WriteLine("Сдача не требуется");
            }
        }
        
        public void ProcessRefund() => 
            Console.WriteLine("Нет необходимости в возврате");
        
        public void EnterMaintenanceMode() => 
            Console.WriteLine("Завершите текущую транзакцию");
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Не в режиме обслуживания");
        
        public bool CanSelectTicket() => false;
        public bool CanInsertMoney() => false;
        public bool CanCancelTransaction() => true;
        public bool CanDispenseTicket() => true;
        public bool CanDispenseChange() => _context.InsertedMoney > _context.SelectedTicket.Price;
        
        public string GetStatusMessage() => $"Средства получены. Ожидание выдачи билета. Сдача: {_context.InsertedMoney - _context.SelectedTicket.Price:C}";
    }

    public class TicketDispensingState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination) => 
            Console.WriteLine("Идет выдача билета");
        
        public void InsertMoney(decimal amount) => 
            Console.WriteLine("Идет выдача билета");
        
        public void CancelTransaction() => 
            Console.WriteLine("Невозможно отменить - идет выдача");
        
        public void DispenseTicket()
        {
            // Имитация выдачи билета
            bool success = _context.DispensePhysicalTicket();
            
            if (success)
            {
                _context.ChangeState(new TicketDispensedState());
                Console.WriteLine("Билет успешно выдан");
            }
            else
            {
                _context.ChangeState(new ErrorState());
                Console.WriteLine("Ошибка при выдаче билета");
            }
        }
        
        public void DispenseChange() => 
            Console.WriteLine("Сначала получите билет");
        
        public void ProcessRefund() => 
            Console.WriteLine("Идет выдача билета");
        
        public void EnterMaintenanceMode() => 
            Console.WriteLine("Завершите текущую транзакцию");
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Не в режиме обслуживания");
        
        public bool CanSelectTicket() => false;
        public bool CanInsertMoney() => false;
        public bool CanCancelTransaction() => false;
        public bool CanDispenseTicket() => true;
        public bool CanDispenseChange() => false;
        
        public string GetStatusMessage() => "Выдача билета...";
    }

    public class TicketDispensedState : IVendingMachineState
    {
        private TicketVendingMachine _context;
        
        public void SetContext(TicketVendingMachine context) => _context = context;
        
        public void SelectTicket(TicketType ticketType, string destination)
        {
            Console.WriteLine("Транзакция завершена. Начинаем новую...");
            _context.ResetTransaction();
            _context.CurrentState.SelectTicket(ticketType, destination);
        }
        
        public void InsertMoney(decimal amount) => 
            Console.WriteLine("Транзакция завершена");
        
        public void CancelTransaction() => 
            Console.WriteLine("Транзакция уже завершена");
        
        public void DispenseTicket() => 
            Console.WriteLine("Билет уже выдан");
        
        public void DispenseChange()
        {
            if (_context.InsertedMoney > _context.SelectedTicket.Price)
            {
                _context.ChangeState(new ChangeDispensingState());
                Console.WriteLine("Выдача сдачи...");
            }
            else
            {
                Console.WriteLine("Сдача не требуется");
                _context.ChangeState(new IdleState());
            }
        }
        
        public void ProcessRefund() => 
            Console.WriteLine("Транзакция завершена успешно");
        
        public void EnterMaintenanceMode() => 
            Console.WriteLine("Завершите работу автомата");
        
        public void ExitMaintenanceMode() => 
            Console.WriteLine("Не в режиме обслуживания");
        
        public bool CanSelectTicket() => true;
        public bool CanInsertMoney() => false;
        public bool CanCancelTransaction() => false;
        public bool CanDispenseTicket() => false;
        public bool CanDispenseChange() => _context.InsertedMoney > _context.SelectedTicket.Price;
        
        public string GetStatusMessage() => "Билет выдан. Получите сдачу или выберите новый билет.";
    }

    // Остальные состояния (ChangeDispensingState, ChangeDispensedState, TransactionCanceledState, 
    // ErrorState, MaintenanceModeState, RefundProcessingState) реализуются аналогично

    // Основной класс автомата
    public class TicketVendingMachine
    {
        public VendingMachineState CurrentState { get; private set; }
        private IVendingMachineState _currentStateObject;
        
        public Ticket SelectedTicket { get; set; }
        public decimal InsertedMoney { get; set; }
        public Dictionary<TicketType, decimal> TicketPrices { get; private set; }
        public List<Ticket> AvailableTickets { get; private set; }
        public decimal AvailableChange { get; private set; }
        public int TicketInventory { get; private set; }
        
        public TicketVendingMachine()
        {
            CurrentState = VendingMachineState.Idle;
            _currentStateObject = new IdleState();
            ((dynamic)_currentStateObject).SetContext(this);
            
            TicketPrices = new Dictionary<TicketType, decimal>
            {
                { TicketType.Adult, 100m },
                { TicketType.Child, 50m },
                { TicketType.Student, 70m },
                { TicketType.Senior, 80m },
                { TicketType.VIP, 200m }
            };
            
            AvailableTickets = new List<Ticket>();
            InsertedMoney = 0m;
            AvailableChange = 500m; // Начальная сдача в автомате
            TicketInventory = 50; // Начальный запас билетов
            
            InitializeTickets();
        }
        
        private void InitializeTickets()
        {
            var destinations = new[] { "Центр", "Аэропорт", "Вокзал", "Стадион", "Театр" };
            
            foreach (var type in Enum.GetValues(typeof(TicketType)).Cast<TicketType>())
            {
                foreach (var destination in destinations)
                {
                    AvailableTickets.Add(new Ticket(type, TicketPrices[type], destination));
                }
            }
        }
        
        public void ChangeState(IVendingMachineState newState)
        {
            _currentStateObject = newState;
            ((dynamic)_currentStateObject).SetContext(this);
            
            // Обновляем перечисление состояния
            CurrentState = newState switch
            {
                IdleState => VendingMachineState.Idle,
                WaitingForMoneyState => VendingMachineState.WaitingForMoney,
                PartialMoneyReceivedState => VendingMachineState.PartialMoneyReceived,
                MoneyReceivedState => VendingMachineState.MoneyReceived,
                TicketDispensingState => VendingMachineState.TicketDispensing,
                TicketDispensedState => VendingMachineState.TicketDispensed,
                _ => CurrentState
            };
        }
        
        // Методы для управления автоматом
        public void SelectTicket(TicketType ticketType, string destination)
        {
            if (_currentStateObject.CanSelectTicket())
            {
                _currentStateObject.SelectTicket(ticketType, destination);
            }
            else
            {
                Console.WriteLine("Невозможно выбрать билет в текущем состоянии");
            }
        }
        
        public void InsertMoney(decimal amount)
        {
            if (_currentStateObject.CanInsertMoney())
            {
                _currentStateObject.InsertMoney(amount);
            }
            else
            {
                Console.WriteLine("Невозможно внести деньги в текущем состоянии");
            }
        }
        
        public void CancelTransaction()
        {
            if (_currentStateObject.CanCancelTransaction())
            {
                _currentStateObject.CancelTransaction();
            }
            else
            {
                Console.WriteLine("Невозможно отменить транзакцию в текущем состоянии");
            }
        }
        
        public void DispenseTicket()
        {
            if (_currentStateObject.CanDispenseTicket())
            {
                _currentStateObject.DispenseTicket();
            }
            else
            {
                Console.WriteLine("Невозможно выдать билет в текущем состоянии");
            }
        }
        
        public void DispenseChange()
        {
            if (_currentStateObject.CanDispenseChange())
            {
                _currentStateObject.DispenseChange();
            }
            else
            {
                Console.WriteLine("Невозможно выдать сдачу в текущем состоянии");
            }
        }
        
        public Ticket GetTicketInfo(TicketType ticketType, string destination)
        {
            return AvailableTickets
                .FirstOrDefault(t => t.Type == ticketType && t.Destination == destination);
        }
        
        public bool DispensePhysicalTicket()
        {
            // Имитация физической выдачи билета
            if (TicketInventory > 0)
            {
                TicketInventory--;
                Console.WriteLine($"Билет напечатан: {SelectedTicket}");
                return true;
            }
            else
            {
                Console.WriteLine("Билеты закончились!");
                return false;
            }
        }
        
        public void ResetTransaction()
        {
            InsertedMoney = 0m;
            SelectedTicket = null;
            ChangeState(new IdleState());
        }
        
        public void DisplayStatus()
        {
            Console.WriteLine($"\n=== СТАТУС АВТОМАТА ===");
            Console.WriteLine($"Состояние: {CurrentState}");
            Console.WriteLine($"Сообщение: {_currentStateObject.GetStatusMessage()}");
            Console.WriteLine($"Внесено денег: {InsertedMoney:C}");
            Console.WriteLine($"Доступная сдача: {AvailableChange:C}");
            Console.WriteLine($"Билетов в наличии: {TicketInventory}");
            
            if (SelectedTicket != null)
            {
                Console.WriteLine($"Выбранный билет: {SelectedTicket}");
            }
        }
    }

    // Пример использования
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== АВТОМАТ ПО ПРОДАЖЕ БИЛЕТОВ ===\n");
            
            var machine = new TicketVendingMachine();
            machine.DisplayStatus();
            
            // Сценарий 1: Успешная покупка билета
            Console.WriteLine("\n=== СЦЕНАРИЙ 1: УСПЕШНАЯ ПОКУПКА ===");
            
            Console.WriteLine("\n1. Выбор билета:");
            machine.SelectTicket(TicketType.Adult, "Центр");
            machine.DisplayStatus();
            
            Console.WriteLine("\n2. Внесение денег (частично):");
            machine.InsertMoney(50m);
            machine.DisplayStatus();
            
            Console.WriteLine("\n3. Внесение остатка:");
            machine.InsertMoney(50m);
            machine.DisplayStatus();
            
            Console.WriteLine("\n4. Выдача билета:");
            machine.DispenseTicket();
            machine.DisplayStatus();
            
            Console.WriteLine("\n5. Выдача сдачи (если требуется):");
            machine.DispenseChange();
            machine.DisplayStatus();
            
            // Сценарий 2: Отмена транзакции
            Console.WriteLine("\n=== СЦЕНАРИЙ 2: ОТМЕНА ТРАНЗАКЦИИ ===");
            machine.ResetTransaction();
            
            Console.WriteLine("\n1. Выбор билета:");
            machine.SelectTicket(TicketType.Child, "Аэропорт");
            machine.DisplayStatus();
            
            Console.WriteLine("\n2. Внесение части денег:");
            machine.InsertMoney(30m);
            machine.DisplayStatus();
            
            Console.WriteLine("\n3. Отмена транзакции:");
            machine.CancelTransaction();
            machine.DisplayStatus();
            
            // Сценарий 3: Покупка с переплатой
            Console.WriteLine("\n=== СЦЕНАРИЙ 3: ПОКУПКА С ПЕРЕПЛАТОЙ ===");
            machine.ResetTransaction();
            
            Console.WriteLine("\n1. Выбор VIP билета:");
            machine.SelectTicket(TicketType.VIP, "Стадион");
            machine.DisplayStatus();
            
            Console.WriteLine("\n2. Внесение больше денег:");
            machine.InsertMoney(250m);
            machine.DisplayStatus();
            
            Console.WriteLine("\n3. Выдача билета:");
            machine.DispenseTicket();
            machine.DisplayStatus();
            
            Console.WriteLine("\n4. Выдача сдачи:");
            machine.DispenseChange();
            machine.DisplayStatus();
            
            // Сценарий 4: Техническое обслуживание
            Console.WriteLine("\n=== СЦЕНАРИЙ 4: ТЕХНИЧЕСКОЕ ОБСЛУЖИВАНИЕ ===");
            machine.ResetTransaction();
            
            Console.WriteLine("\n1. Вход в режим обслуживания:");
            // Реализуйте метод для входа в режим обслуживания
            
            Console.WriteLine("\nПрограмма завершена.");
        }
    }
}