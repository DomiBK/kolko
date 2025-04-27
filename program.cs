using System.Net;
using System.Net.Sockets;
using System.Text;

class TicTacToeServer
{
    static TcpClient? player1 = null;
    static TcpClient? player2 = null;
    static char[] board = new char[9];
    static char currentPlayer = 'X';

    static void Main()
    {
        Array.Fill(board, ' ');
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Serwer uruchomiony na porcie 5000...");

        player1 = listener.AcceptTcpClient();
        Console.WriteLine("Gracz 1 połączony.");
        Send(player1, "START|X");

        player2 = listener.AcceptTcpClient();
        Console.WriteLine("Gracz 2 połączony.");
        Send(player2, "START|O");

        Thread player1Thread = new Thread(() => HandleClient(player1, player2, 'X'));
        Thread player2Thread = new Thread(() => HandleClient(player2, player1, 'O'));

        player1Thread.Start();
        player2Thread.Start();
    }

    static void HandleClient(TcpClient player, TcpClient opponent, char symbol)
    {
        var stream = player.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int length = stream.Read(buffer, 0, buffer.Length);
            if (length == 0) continue;

            string message = Encoding.UTF8.GetString(buffer, 0, length);
            if (message.StartsWith("MOVE|"))
            {
                int index = int.Parse(message.Substring(5));

                lock (board)
                {
                    if (currentPlayer != symbol)
                    {
                        Send(player, "ERROR|Not your turn");
                        continue;
                    }

                    if (index < 0 || index > 8 || board[index] != ' ')
                    {
                        Send(player, "ERROR|Invalid move");
                        continue;
                    }

                    board[index] = symbol;
                    currentPlayer = (currentPlayer == 'X') ? 'O' : 'X';

                    string state = $"UPDATE|{new string(board)}|{CheckGameState()}";
                    Send(player, state);
                    Send(opponent, state);
                }
            }
        }
    }

    static void Send(TcpClient client, string message)
    {
        var stream = client.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    static string CheckGameState()
    {
        int[][] wins = new int[][] {
            new[]{0,1,2}, new[]{3,4,5}, new[]{6,7,8},
            new[]{0,3,6}, new[]{1,4,7}, new[]{2,5,8},
            new[]{0,4,8}, new[]{2,4,6}
        };

        foreach (var combo in wins)
        {
            if (board[combo[0]] != ' ' &&
                board[combo[0]] == board[combo[1]] &&
                board[combo[1]] == board[combo[2]])
            {
                return $"WIN|{board[combo[0]]}";
            }
        }

        if (!board.Contains(' '))
            return "DRAW";

        return "CONTINUE";
    }
}
