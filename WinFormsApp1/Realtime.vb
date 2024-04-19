Imports System.Windows.Forms.AxHost
Imports Newtonsoft.Json
Imports Supabase.Realtime
Imports Supabase.Realtime.Interfaces
Imports Supabase.Realtime.Models
Public Class RealtimeService
	Private ReadOnly Property Client As Supabase.Client
	Private Const Url As String = "https://urlgoeshere.supabase.co"
	Private Const Key As String = "key.goes.here"

	Private presence As RealtimePresence(Of UserPresence)

#Region " Singleton "

	Private Shared instance As RealtimeService
	Private Sub New()
	End Sub
	Public Shared ReadOnly Property GetInstance As RealtimeService
		Get
			If instance Is Nothing Then instance = New RealtimeService
			Return instance
		End Get
	End Property


#End Region

	Public Async Function GetClient() As Task(Of Supabase.Client)
		Try

			If _Client IsNot Nothing Then Return _Client

			_Client = New Supabase.Client(Url, Key, New Supabase.SupabaseOptions With {.AutoConnectRealtime = True})

			_Client = Await _Client.InitializeAsync()

			'  Dim p = Await _Client.Auth.SignIn(emailAddress, encryptedPassword)

			Return _Client
		Catch ex As Exception
			_Client = Nothing
			Return Nothing
		End Try
	End Function

	Public Async Function Register() As Task

		Dim presenceId = Guid.NewGuid().ToString()

		Dim client = Await GetClient()

		Await signin()

		client.Realtime.AddDebugHandler(Sub(sender, message, exception)
											Debug.WriteLine(message)
										End Sub)

		Dim Channel = client.Realtime.Channel("11111111111111111111111111111112")
		presence = Channel.Register(Of UserPresence)(presenceId)

		presence.AddPresenceEventHandler(Interfaces.IRealtimePresence.EventType.Sync, Sub(s, e)
																						  For Each state In Presence.CurrentState
																							  Dim UserID = state.Key
																							  Dim LastSeen = state.Value.First.LastSeen
																							  Debug.WriteLine($"SYNC: {UserID} - {LastSeen:F}")
																						  Next
																					  End Sub)

		Presence.AddPresenceEventHandler(Interfaces.IRealtimePresence.EventType.Join, Sub(s, e)
																						  For Each state In Presence.CurrentState
																							  Dim UserID = state.Key
																							  Dim LastSeen = state.Value.First.LastSeen
																							  Debug.WriteLine($"JOIN: {UserID} - {LastSeen:F}")
																						  Next
																					  End Sub)

		Presence.AddPresenceEventHandler(Interfaces.IRealtimePresence.EventType.Leave, Sub(s, e)
																						   For Each state In Presence.CurrentState
																							   Dim UserID = state.Key
																							   Dim LastSeen = state.Value.First.LastSeen
																							   Debug.WriteLine($"LEAVE: {UserID} - {LastSeen:F}")
																						   Next
																					   End Sub)

		Await Channel.Subscribe()
		Track()
	End Function

	Public Sub Track()
		presence.Track(New UserPresence With {.LastSeen = DateTime.Now})
	End Sub

	Public Async Function signin() As Task
		Await Client.Auth.SignIn("darren@email.com", "MyPassword")
	End Function

End Class
Public Class UserPresence
	Inherits BasePresence

	<JsonProperty("lastSeen")>
	Public Property LastSeen As Date

End Class
