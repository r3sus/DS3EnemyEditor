﻿using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DS3EnemyEditor
{
    class EnemyDataManager
    {
        private DataGridView dataGridView;

        private MSB3 msb3;
        private List<MSB3.Part.Enemy> enemies;

        public EnemyDataManager(DataGridView dataGridView)
        {
            this.dataGridView = dataGridView;
        }

        public void InitializeTable()
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;

            dataGridView.CellValidating += CellValidatingHandler;
            dataGridView.CellValidated += CellValidatedHandler;

            DataGridViewColumnCollection columns = dataGridView.Columns;
            DataGridViewColumn column;
            DataGridViewTextBoxCell template = new DataGridViewTextBoxCell();

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "Name";
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "ModelName";
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "ThinkParamID";
            column.ToolTipText = "Controls enemy AI";
            column.ValueType = typeof(int);
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "NPCParamID";
            column.ToolTipText = "Controls enemy stats";
            column.ValueType = typeof(int);
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "EventEntityID";
            column.ToolTipText = "Used to identify the part in event scripts";
            column.ValueType = typeof(int);
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "TalkID";
            column.ToolTipText = "Controls enemy speech";
            column.ValueType = typeof(int);
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "CharaInitID";
            column.ToolTipText = "Controls enemy equipment";
            column.ValueType = typeof(int);
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "Position";
            columns.Add(column);

            column = new DataGridViewColumn(template);
            column.Name = column.HeaderText = "Rotation";
            columns.Add(column);

            dataGridView.AutoResizeColumns();
        }

        public void LoadMSB3(String filename)
        {
            msb3 = SoulsFile<MSB3>.Read(filename);
            enemies = msb3.Parts.Enemies;
            PopulateTable();
        }
        public void SaveMSB3(String filename)
        {
            msb3.Write(filename);
        }

        public void DeleteSelected()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    int index = row.Index;
                    dataGridView.Rows.RemoveAt(index);
                    enemies.RemoveAt(index);
                }
            } else if (dataGridView.SelectedCells.Count == 1)
            {
                int index = dataGridView.SelectedCells[0].RowIndex;
                dataGridView.Rows.RemoveAt(index);
                enemies.RemoveAt(index);
            } else
            {
                return;
            }

            UpdateRowHeaders();
        }

        public void DuplicateSelected()
        {
            DataGridViewRow row;

            if (dataGridView.SelectedRows.Count == 1)
            {
                row = dataGridView.SelectedRows[0];
            } else if (dataGridView.SelectedCells.Count == 1)
            {
                row = dataGridView.Rows[dataGridView.SelectedCells[0].RowIndex];
            } else
            {
                return;
            }

            int index = row.Index;

            DataGridViewRow newRow = (DataGridViewRow) row.Clone();
            for (int i = 0; i < row.Cells.Count; i++)
            {
                newRow.Cells[i].Value = row.Cells[i].Value;
            }
            dataGridView.Rows.Insert(index + 1, newRow);

            enemies.Insert(index + 1, new MSB3.Part.Enemy(enemies[index]));

            UpdateRowHeaders();
        }

        private void PopulateTable()
        {
            dataGridView.CellValueChanged -= CellValueChangedHandler;

            DataGridViewRowCollection rows = dataGridView.Rows;
            rows.Clear();

            foreach (MSB3.Part.Enemy enemy in enemies)
            {
                DataGridViewRow row = rows[rows.Add()];

                row.Cells["Name"].Value = enemy.Name;
                row.Cells["ModelName"].Value = enemy.ModelName;
                row.Cells["ThinkParamID"].Value = enemy.ThinkParamID;
                row.Cells["NPCParamID"].Value = enemy.NPCParamID;
                row.Cells["EventEntityID"].Value = enemy.EventEntityID;
                row.Cells["TalkID"].Value = enemy.TalkID;
                row.Cells["CharaInitID"].Value = enemy.CharaInitID;
                row.Cells["Position"].Value = enemy.Position.ToString();
                row.Cells["Rotation"].Value = enemy.Rotation.ToString();
            }

            UpdateRowHeaders();
            dataGridView.AutoResizeColumns();

            dataGridView.CellValueChanged += CellValueChangedHandler;
        }

        private void UpdateRowHeaders()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                row.HeaderCell.Value = String.Format("{0}", row.Index + 1);
            }
        }

        private void CellValidatingHandler(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridViewColumn column = dataGridView.Columns[e.ColumnIndex];
            DataGridViewRow row = dataGridView.Rows[e.RowIndex];
            DataGridViewCell cell = row.Cells[e.ColumnIndex];

            switch (column.Name)
            {
                case "ThinkParamID":
                case "NPCParamID":
                case "EventEntityID":
                case "TalkID":
                case "CharaInitID":
                    if (!int.TryParse(e.FormattedValue.ToString(), out _))
                    {
                        row.ErrorText = "Invalid integer";
                        e.Cancel = true;
                    }
                    break;

                case "Position":
                case "Rotation":
                    try
                    {
                        ParseVector3(e.FormattedValue.ToString());
                    } catch (Exception)
                    {
                        row.ErrorText = "Invalid vector3";
                        e.Cancel = true;
                    }
                    break;
            }
        }

        private void CellValidatedHandler(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].ErrorText = null;
        }

        private void CellValueChangedHandler(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0) return;

            DataGridViewColumn column = dataGridView.Columns[e.ColumnIndex];
            DataGridViewRow row = dataGridView.Rows[e.RowIndex];
            DataGridViewCell cell = row.Cells[e.ColumnIndex];
            MSB3.Part.Enemy enemy = enemies[e.RowIndex];

            switch (column.Name)
            {
                case "Name":
                    enemy.Name = (string) cell.Value;
                    break;

                case "ModelName":
                    enemy.ModelName = (string) cell.Value;
                    break;

                case "ThinkParamID":
                    enemy.ThinkParamID = (int) cell.Value;
                    break;

                case "NPCParamID":
                    enemy.NPCParamID = (int) cell.Value;
                    break;

                case "EventEntityID":
                    enemy.EventEntityID = (int) cell.Value;
                    break;

                case "TalkID":
                    enemy.TalkID = (int) cell.Value;
                    break;

                case "CharaInitID":
                    enemy.CharaInitID = (int)cell.Value;
                    break;

                case "Position":
                    enemy.Position = ParseVector3((string) cell.Value);
                    break;

                case "Rotation":
                    enemy.Rotation = ParseVector3((string)cell.Value);
                    break;

                default:
                    throw new Exception("CellValueChangedHandler missing code path");
            }
        }

        private static Vector3 ParseVector3(string input)
        {
            char[] charsToTrim = { '<', '>', ' ', '\t' };
            string[] vs = input.Split(',').Select(s => s.Trim(charsToTrim)).ToArray();
            if (vs.Length == 3)
            {
                return new Vector3(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]));
            } else
            {
                throw new FormatException("Invalid format for Vector3");
            }
        }
    }
}
