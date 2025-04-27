using System.Net.Sockets;
using System.Text;

namespace TicTacToeClient;

public partial class MainPage : ContentPage
{
    TcpClient client = new();
    NetworkStream stream;
    char mySymbol;
    char[] board = new char[9];
    Button[] buttons = new Button[9];

    public MainPage()
    {
        InitializeComponent();
        ConnectToServer();
        InitializeBoard();
    }

    void InitializeBoard()
    {
        for (int i = 0; i < 9; i++)
        {
            var btn = new Button
            {
                FontSize = 40,
                BackgroundColor = Colors.LightGray
            };
            btn.Clicked += OnButtonClicked;
            buttons[i] = btn;
            GameGrid.Add(btn, i % 3, i / 3);
        }
    }

    async void ConnectToServer()
    {
        await client.ConnectAsync("127.0.0.1", 5000);
        stream = client.GetStream();

        _ = Task.Run(ReceiveMessages);
    }

    async void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (length == 0) continue;

            string message = Encoding.UTF8.GetString(buffer, 0, length);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (message.StartsWith("START|"))
                {
                    mySymbol = message[6];
                    StatusLabel.Text = $"Grasz jako {mySymbol}";
                }
                else if (message.StartsWith("UPDATE|"))
                {
                    var parts = message.Split('|');
                    board = parts[1].ToCharArray();
                    string state = parts[2];

                    for (int i = 0; i < 9; i++)
                    {
                        buttons[i].Text = board[i].ToString();
                    }

                    if (state.StartsWith("WIN"))
                    {
                        StatusLabel.Text = $"Wygrał {state[4]}!";
                    }
                    else if (state == "DRAW")
                    {
                        StatusLabel.Text = "Remis!";
                    }
                    else
                    {
                        StatusLabel.Text = "Twoja kolej!";
                    }
                }
                else if (message.StartsWith("ERROR|"))
                {
                    StatusLabel.Text = message.Substring(6);
                }
            });
        }
    }

    void OnButtonClicked(object sender, EventArgs e)
    {
        var index = Array.IndexOf(buttons, sender as Button);
        byte[] data = Encoding.UTF8.GetBytes($"MOVE|{index}");
        stream.Write(data, 0, data.Length);
    }
}
