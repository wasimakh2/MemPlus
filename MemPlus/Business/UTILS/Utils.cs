﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.Windows;
using MemPlus.Business.EXPORT;
using MemPlus.Business.LOG;
using MemPlus.Business.PROCESS;
using MemPlus.Business.RAM;
using Microsoft.Win32;

namespace MemPlus.Business.UTILS
{
    /// <summary>
    /// Static class containing utility code that can be used by different objects
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Check if the application is running with Administrative rights
        /// </summary>
        /// <returns>True if the application has Administrative rights, otherwise false</returns>
        internal static bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Retrieve RAM information
        /// </summary>
        /// <returns>A list of RAM information</returns>
        internal static List<RamStick> GetRamSticks()
        {
            List<RamStick> ramSticks = new List<RamStick>();

            ConnectionOptions connection = new ConnectionOptions { Impersonation = ImpersonationLevel.Impersonate };

            ManagementScope scope = new ManagementScope("\\root\\CIMV2", connection);
            scope.Connect();

            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_PhysicalMemory");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (ManagementObject queryObj in searcher.Get())
            {
                RamStick stick = new RamStick();
                foreach (PropertyData data in queryObj.Properties)
                {
                    if (data.Value != null)
                    {
                        stick.AddRamData(new RamData(data.Name, data.Value.ToString()));
                    }
                }

                ramSticks.Add(stick);
            }

            return ramSticks;
        }

        /// <summary>
        /// Export all RamStick objects
        /// </summary>
        /// <param name="logController">The LogController object that can be used to add logs</param>
        internal static void ExportRamSticks(LogController logController)
        {
            List<RamStick> ramSticks = GetRamSticks();
            if (ramSticks == null || ramSticks.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt|HTML file (*.html)|*.html|CSV file (*.csv)|*.csv|Excel file (*.csv)|*.csv"
            };
            if (sfd.ShowDialog() != true) return;
            try
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (sfd.FilterIndex)
                {
                    //Filterindex starts at 1
                    case 1:
                        RamDataExporter.ExportText(sfd.FileName, ramSticks);
                        break;
                    case 2:
                        RamDataExporter.ExportHtml(sfd.FileName, ramSticks);
                        break;
                    case 3:
                        RamDataExporter.ExportCsv(sfd.FileName, ramSticks);
                        break;
                    case 4:
                        RamDataExporter.ExportExcel(sfd.FileName, ramSticks);
                        break;
                }
                MessageBox.Show("Exported all data!", "MemPlus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export logs to the disk
        /// </summary>
        /// <param name="logType">The LogType that should be exported (can be null to export all logs)</param>
        /// <param name="logController">The LogController object that can be used to export logs</param>
        internal static void ExportLogs(LogType? logType, LogController logController)
        {
            if (logType != null)
            {
                if (logController.GetLogs(logType).Count == 0) return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt|HTML file (*.html)|*.html|CSV file (*.csv)|*.csv|Excel file (*.csv)|*.csv"
            };

            if (sfd.ShowDialog() != true) return;
            ExportTypes.ExportType type;
            switch (sfd.FilterIndex)
            {
                default:
                    type = ExportTypes.ExportType.Text;
                    break;
                case 2:
                    type = ExportTypes.ExportType.Html;
                    break;
                case 3:
                    type = ExportTypes.ExportType.Csv;
                    break;
                case 4:
                    type = ExportTypes.ExportType.Excel;
                    break;
            }

            try
            {
                logController.Export(sfd.FileName, logType, type);
                MessageBox.Show("All logs have been exported!", "MemPlus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export all ProcessDetail objects
        /// </summary>
        /// <param name="logController">The LogController object that can be used to add logs</param>
        internal static void ExportProcessDetails(LogController logController)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text file (*.txt)|*.txt|HTML file (*.html)|*.html|CSV file (*.csv)|*.csv|Excel file (*.csv)|*.csv"
            };
            if (sfd.ShowDialog() != true) return;
            try
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (sfd.FilterIndex)
                {
                    //Filterindex starts at 1
                    case 1:
                        ProcessDetailExporter.ExportText(sfd.FileName, GetProcessDetails(logController));
                        break;
                    case 2:
                        ProcessDetailExporter.ExportHtml(sfd.FileName, GetProcessDetails(logController));
                        break;
                    case 3:
                        ProcessDetailExporter.ExportCsv(sfd.FileName, GetProcessDetails(logController));
                        break;
                    case 4:
                        ProcessDetailExporter.ExportExcel(sfd.FileName, GetProcessDetails(logController));
                        break;
                }
                MessageBox.Show("All data has been exported!", "MemPlus", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                logController.AddLog(new ApplicationLog(ex.Message));
                MessageBox.Show(ex.Message, "MemPlus", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Retrieve a list of ProcessDetail objects
        /// </summary>
        /// <param name="logController">The LogController object that can be used to add logs</param>
        /// <returns></returns>
        internal static List<ProcessDetail> GetProcessDetails(LogController logController)
        {
            logController.AddLog(new ProcessLog("Retrieving process details"));
            List<ProcessDetail> processDetailsList = new List<ProcessDetail>();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    ProcessDetail pd = new ProcessDetail
                    {
                        ProcessId = p.Id,
                        ProcessName = p.ProcessName,
                        ProcessLocation = p.MainModule.FileName,
                        MemoryUsage = (p.WorkingSet64 / (1024 * 1024)).ToString("F2") + " MB",
                        Handle = p.Handle
                    };
                    processDetailsList.Add(pd);
                }
                catch (Exception ex)
                {
                    logController.AddLog(new ProcessLog(p.ProcessName + ": " + ex.Message));
                }
            }
            logController.AddLog(new ProcessLog("Done retrieving process details"));
            return processDetailsList;
        }
    }
}
