using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace DeleteTfsProject
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        }

        private void Button_Click_1( object sender, RoutedEventArgs e )
        {
            listProjectNames.Items.Clear();

            //"using" pattern is recommended as the picker needs to be disposed of
            using ( TeamProjectPicker tpp = new TeamProjectPicker( TeamProjectPickerMode.MultiProject, false ) ) {
                var result = tpp.ShowDialog();
                if ( result == System.Windows.Forms.DialogResult.OK ) {
                    textBlockServerName.Text = tpp.SelectedTeamProjectCollection.Name;

                    foreach ( ProjectInfo projectInfo in tpp.SelectedProjects ) {
                        listProjectNames.Items.Add( projectInfo.Name );
                    }
                }
            }
        }

        string serverName;
        string projectName;
        bool isHttps;
        BackgroundWorker bw = new BackgroundWorker();

        private void Button_Click_2( object sender, RoutedEventArgs e )
        {
            try {
                if ( listProjectNames.SelectedIndex == -1 ) {
                    throw new Exception( "削除するプロジェクトを選択してください。" );
                }

                serverName = textBlockServerName.Text;
                projectName = listProjectNames.Items[listProjectNames.SelectedIndex] as string;
                var result = MessageBox.Show( string.Format( "{0}の{1}を削除します。\n削除すると元に戻せません。よろしいですか？",
                    serverName, projectName ), "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No );
                if ( result == MessageBoxResult.No ) {
                    return;
                }

                isHttps = checkIsHttps.IsChecked == true;

                deketeProgress.IsIndeterminate = true;

                buttonSelectTFS.IsEnabled = false;
                buttonDelete.IsEnabled = false;

                bw.RunWorkerAsync();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
            }
        }

        void bw_DoWork( object sender, DoWorkEventArgs e )
        {
            // http://www.atmarkit.co.jp/fdotnet/dotnettips/657redirectstdout/redirectstdout.html
            var arg = string.Format( "/c VsDevCmdAndCmd.bat tfsdeleteproject /q /collection:{2}://{0} \"{1}\"",
                serverName, projectName, (isHttps ? "https" : "http") );

            var process = new Process();
            process.StartInfo.FileName = "cmd.exe"; // 実行するファイル
            process.StartInfo.Arguments = arg;
            process.StartInfo.CreateNoWindow = true; // コンソールを開かない
            //process.StartInfo.CreateNoWindow = false; // コンソールを開かない
            process.StartInfo.UseShellExecute = false; // シェル機能を使用しない
            process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト

            process.Start(); // アプリの実行開始

            e.Result = process.StandardOutput.ReadToEnd().Replace( "\r\r\n", "\n" ); // 標準出力の読み取り&改行コードの修正
        }

        void bw_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            deketeProgress.IsIndeterminate = false;

            buttonSelectTFS.IsEnabled = true;
            buttonDelete.IsEnabled = true;

            if ( e.Error != null ) {
                MessageBox.Show( e.Error.Message );
            }
            else {
                MessageBox.Show( e.Result as string );
            }
        }
    }
}
