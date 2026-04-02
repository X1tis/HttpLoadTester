using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using HttpLoadTester.Data;
using HttpLoadTester.Infrastructure;
using HttpLoadTester.Models;
using HttpLoadTester.Services;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace HttpLoadTester.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly TestConfigurationRepository _configRepository;
        private readonly TestResultRepository _resultRepository;
        private readonly LoadTestEngine _loadTestEngine;
        private readonly ExportService _exportService;

        private TestConfiguration _selectedSavedConfiguration;
        private TestConfiguration _currentConfiguration;
        private TestResult _selectedResult;
        private bool _isRunning;
        private int _progressPercent;
        private string _statusMessage;
        private double _liveRps;
        private double _liveAverageResponse;
        private double _successRate;
        private int _totalRequests;
        private CancellationTokenSource _cts;

        public ObservableCollection<TestConfiguration> Configurations { get; }
        public ObservableCollection<TestResult> Results { get; }
        public ObservableCollection<RequestLog> RecentLogs { get; }
        public SeriesCollection ResponseSeries { get; }

        public ICommand LoadConfigurationsCommand { get; }
        public ICommand SaveConfigurationCommand { get; }
        public ICommand DeleteConfigurationCommand { get; }
        public ICommand StartTestCommand { get; }
        public ICommand StopTestCommand { get; }
        public ICommand LoadResultsCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }

        public MainViewModel()
        {
            _configRepository = new TestConfigurationRepository();
            _resultRepository = new TestResultRepository();
            _loadTestEngine = new LoadTestEngine();
            _exportService = new ExportService();

            Configurations = new ObservableCollection<TestConfiguration>();
            Results = new ObservableCollection<TestResult>();
            RecentLogs = new ObservableCollection<RequestLog>();

            CurrentConfiguration = new TestConfiguration
            {
                Id = 0,
                TestName = "Новый тест",
                Url = "https://httpbin.org/get",
                HttpMethod = "GET",
                Headers = "{\r\n  \"Accept\": \"application/json\"\r\n}",
                Body = "",
                ConcurrentUsers = 10,
                DurationSeconds = 20,
                RampUpSeconds = 2,
                TimeoutMilliseconds = 10000,
                Description = "Базовая конфигурация"
            };

            ResponseSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Время ответа, мс",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometrySize = 4,
                    LineSmoothness = 0.2
                }
            };

            LoadConfigurationsCommand = new RelayCommand(_ => LoadConfigurations());
            SaveConfigurationCommand = new RelayCommand(_ => SaveConfiguration());
            DeleteConfigurationCommand = new RelayCommand(_ => DeleteConfiguration(), _ => SelectedSavedConfiguration != null);
            StartTestCommand = new RelayCommand(async _ => await StartTestAsync(), _ => !IsRunning);
            StopTestCommand = new RelayCommand(_ => StopTest(), _ => IsRunning);
            LoadResultsCommand = new RelayCommand(_ => LoadResults());
            ExportCsvCommand = new RelayCommand(_ => ExportCsv(), _ => SelectedResult != null);
            ExportJsonCommand = new RelayCommand(_ => ExportJson(), _ => SelectedResult != null);

            try
            {
                LoadConfigurations();
                LoadResults();
                StatusMessage = "Приложение готово к работе.";
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                StatusMessage = "Ошибка подключения к базе данных.";
                MessageBox.Show(
                    "Не удалось подключиться к MySQL.\n\nПроверь:\n1. Запущен ли MySQL\n2. Создана ли база diploma_load_tester\n3. Верны ли логин/пароль в App.config\n4. Установлен ли пакет MySql.Data\n\nТекст ошибки:\n" + ex.Message,
                    "Ошибка БД",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public TestConfiguration SelectedSavedConfiguration
        {
            get => _selectedSavedConfiguration;
            set
            {
                if (SetProperty(ref _selectedSavedConfiguration, value) && value != null)
                {
                    CurrentConfiguration = value.Clone();
                }
            }
        }

        public TestConfiguration CurrentConfiguration
        {
            get => _currentConfiguration;
            set => SetProperty(ref _currentConfiguration, value);
        }

        public TestResult SelectedResult
        {
            get => _selectedResult;
            set => SetProperty(ref _selectedResult, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double LiveRps
        {
            get => _liveRps;
            set => SetProperty(ref _liveRps, value);
        }

        public double LiveAverageResponse
        {
            get => _liveAverageResponse;
            set => SetProperty(ref _liveAverageResponse, value);
        }

        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        public int TotalRequests
        {
            get => _totalRequests;
            set => SetProperty(ref _totalRequests, value);
        }

        private void LoadConfigurations()
        {
            Configurations.Clear();
            foreach (var item in _configRepository.GetAll())
                Configurations.Add(item);

            StatusMessage = "Конфигурации загружены.";
        }

        private void SaveConfiguration()
        {
            try
            {
                ValidateConfiguration();

                if (CurrentConfiguration.Id == 0)
                {
                    CurrentConfiguration.CreatedAt = DateTime.Now;
                    CurrentConfiguration.Id = _configRepository.Insert(CurrentConfiguration);
                }
                else
                {
                    _configRepository.Update(CurrentConfiguration);
                }

                LoadConfigurations();
                StatusMessage = "Конфигурация сохранена.";
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteConfiguration()
        {
            try
            {
                if (SelectedSavedConfiguration == null)
                    return;

                if (MessageBox.Show("Удалить выбранную конфигурацию?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _configRepository.Delete(SelectedSavedConfiguration.Id);
                    SelectedSavedConfiguration = null;

                    CurrentConfiguration = new TestConfiguration
                    {
                        Id = 0,
                        TestName = "Новый тест",
                        Url = "",
                        HttpMethod = "GET",
                        Headers = "{\r\n  \"Accept\": \"application/json\"\r\n}",
                        Body = "",
                        ConcurrentUsers = 10,
                        DurationSeconds = 20,
                        RampUpSeconds = 2,
                        TimeoutMilliseconds = 10000,
                        Description = ""
                    };

                    LoadConfigurations();
                    StatusMessage = "Конфигурация удалена.";
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                MessageBox.Show(ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartTestAsync()
        {
            try
            {
                ValidateConfiguration();

                if (CurrentConfiguration.Id == 0)
                {
                    CurrentConfiguration.CreatedAt = DateTime.Now;
                    CurrentConfiguration.Id = _configRepository.Insert(CurrentConfiguration);
                    LoadConfigurations();
                }

                IsRunning = true;
                ProgressPercent = 0;
                TotalRequests = 0;
                LiveAverageResponse = 0;
                LiveRps = 0;
                SuccessRate = 0;
                StatusMessage = "Тест выполняется...";

                RecentLogs.Clear();
                ((ChartValues<ObservablePoint>)ResponseSeries[0].Values).Clear();

                _cts = new CancellationTokenSource();

                var progress = new Progress<LiveTestSnapshot>(snapshot =>
                {
                    TotalRequests = snapshot.TotalRequests;
                    LiveAverageResponse = Math.Round(snapshot.AverageResponseTimeMs, 2);
                    LiveRps = Math.Round(snapshot.RequestsPerSecond, 2);
                    SuccessRate = snapshot.TotalRequests == 0
                        ? 0
                        : Math.Round(snapshot.SuccessfulRequests * 100.0 / snapshot.TotalRequests, 2);
                    ProgressPercent = snapshot.CompletionPercent;

                    var values = (ChartValues<ObservablePoint>)ResponseSeries[0].Values;
                    values.Add(new ObservablePoint(values.Count + 1, snapshot.LastResponseTimeMs));

                    if (values.Count > 120)
                        values.RemoveAt(0);

                    RecentLogs.Clear();
                    foreach (var log in snapshot.RecentLogs)
                        RecentLogs.Add(log);
                });

                var runResult = await _loadTestEngine.RunAsync(CurrentConfiguration, progress, _cts.Token);

                int resultId = _resultRepository.InsertResult(runResult.Result);
                _resultRepository.InsertLogs(resultId, runResult.Logs);

                LoadResults();
                StatusMessage = "Тест завершён и сохранён в базе данных.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Тест остановлен пользователем.";
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                MessageBox.Show(ex.Message, "Ошибка запуска теста", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsRunning = false;
                ProgressPercent = 100;
                _cts = null;
            }
        }

        private void StopTest()
        {
            _cts?.Cancel();
        }

        private void LoadResults()
        {
            Results.Clear();
            foreach (var item in _resultRepository.GetAllResults())
                Results.Add(item);

            StatusMessage = "История результатов загружена.";
        }

        private void ExportCsv()
        {
            try
            {
                if (SelectedResult == null)
                {
                    MessageBox.Show("Выберите результат.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var logs = _resultRepository.GetLogsByResultId(SelectedResult.Id);

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"result_{SelectedResult.Id}.csv",
                    DefaultExt = ".csv",
                    AddExtension = true
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    _exportService.ExportLogsToCsv(dialog.FileName, logs);
                    MessageBox.Show("CSV экспортирован.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportJson()
        {
            try
            {
                if (SelectedResult == null)
                {
                    MessageBox.Show("Выберите результат.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var logs = _resultRepository.GetLogsByResultId(SelectedResult.Id);

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    FileName = $"result_{SelectedResult.Id}.json",
                    DefaultExt = ".json",
                    AddExtension = true
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    _exportService.ExportResultToJson(dialog.FileName, SelectedResult, logs);
                    MessageBox.Show("JSON экспортирован.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
                MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateConfiguration()
        {
            if (CurrentConfiguration == null)
                throw new Exception("Конфигурация не задана.");

            if (string.IsNullOrWhiteSpace(CurrentConfiguration.TestName))
                throw new Exception("Введите название теста.");

            if (string.IsNullOrWhiteSpace(CurrentConfiguration.Url))
                throw new Exception("Введите URL.");

            Uri uri;
            if (!Uri.TryCreate(CurrentConfiguration.Url, UriKind.Absolute, out uri))
                throw new Exception("Некорректный URL.");

            if (string.IsNullOrWhiteSpace(CurrentConfiguration.HttpMethod))
                throw new Exception("Выберите HTTP-метод.");

            if (CurrentConfiguration.ConcurrentUsers <= 0)
                throw new Exception("Количество пользователей должно быть больше 0.");

            if (CurrentConfiguration.DurationSeconds <= 0)
                throw new Exception("Длительность должна быть больше 0.");

            if (CurrentConfiguration.RampUpSeconds < 0)
                throw new Exception("Ramp-Up не может быть отрицательным.");

            if (CurrentConfiguration.TimeoutMilliseconds <= 0)
                throw new Exception("Таймаут должен быть больше 0.");
        }
    }
}
