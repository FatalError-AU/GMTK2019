using System;
using System.Collections.Generic;
using System.Linq;
using Enemy;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class AiGraphWindow : EditorWindow
{
    public readonly Vector2 nodeSize = new Vector2(200F, 30F);

    private int selectedMachine;
    private Vector2 offset = Vector2.zero;

    private List<AiGraphClient.Machine> machines;

    private void Update()
    {
        Repaint();
    }

    private void OnGUI()
    {
        Rect size = position;
        size.position = Vector2.zero;

        machines = AiGraphClient.GetMachines();

        if (!machines.Any())
        {
            EditorGUI.LabelField(size, "No machines registered, please try running the game", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        RenderToolbar();

        if (selectedMachine >= machines.Count)
            return;

        AiGraphClient.Machine machine = machines[selectedMachine];

        AiStateMachine.MachineSnapshot snapshot = machine.machine.snapshot;

        if (snapshot.positions == null || snapshot.positions.Length != snapshot.states.Length)
        {
            CalculatePositions(ref snapshot);
            machine.machine.snapshot = snapshot;
        }

        Rect clip = size;
        clip.y += 20F;
        clip.height -= 20F;

        GUI.BeginClip(clip);
        Handles.BeginGUI();
        Handles.color = Color.green;
        foreach (Tuple<string, string> t in snapshot.transitions)
        {
            int stateA = Array.IndexOf(snapshot.states, t.Item1);
            int stateB = Array.IndexOf(snapshot.states, t.Item2);
            Handles.DrawLine(
                    snapshot.positions[stateA] * size.size + offset + size.center + nodeSize * .5F,
                    snapshot.positions[stateB] * size.size + offset + size.center + nodeSize * .5F);
        }

        for (int i = 0; i < snapshot.states.Length; i++)
        {
            Tint(snapshot.states[i], snapshot);
            GUI.Box(
                    new Rect(snapshot.positions[i] * size.size + offset + size.center, nodeSize),
                    GetName(snapshot.states[i]), EditorStyles.miniButton);
        }

        Handles.EndGUI();
        GUI.EndClip();

        if (Event.current.type == EventType.MouseDrag)
        {
            GUI.changed = true;
            offset += Event.current.delta;
        }
    }

    private void RenderToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));
        if (selectedMachine >= machines.Count)
            selectedMachine = 0;

        int sel = EditorGUILayout.Popup(selectedMachine, machines.Select(x => x.name).ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(200F));
        if (sel != selectedMachine)
        {
            offset = Vector2.zero;
            selectedMachine = sel;
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(150F)))
            machines[selectedMachine].machine.snapshot.positions = null;
        EditorGUILayout.EndHorizontal();
    }

    private string GetName(string name)
        => AiStateMachine.ANY_STATE.Equals(name) ? "Any State" : name;

    private void Tint(string state, AiStateMachine.MachineSnapshot snapshot)
    {
        GUI.color = new Color(1F, 1F, 1F);

        if (AiStateMachine.ANY_STATE.Equals(state))
            GUI.color *= new Color(1f, 0.74f, 0.5f);
        if (snapshot.currentState.Equals(state) && EditorApplication.isPlaying)
            GUI.color *= new Color(0.5f, 1f, 0.5f);
    }

    #region Force Directed Layout

    private const int ITERATIONS = 10;
    private const float REPULSION_FACTOR = 10F;
    private const float SPEED = 3F;
    private const float AREA = 1F;
    private const float GRAVITY = 9.81F;

    private void CalculatePositions(ref AiStateMachine.MachineSnapshot snapshot)
    {
        offset = Vector2.zero;

        string[] verticies = snapshot.states;

        Dictionary<string, HashSet<string>> edges = new Dictionary<string, HashSet<string>>();

        foreach (Tuple<string, string> edge in snapshot.transitions)
        {
            if (!edges.ContainsKey(edge.Item1))
                edges.Add(edge.Item1, new HashSet<string>());
            if (!edges.ContainsKey(edge.Item2))
                edges.Add(edge.Item2, new HashSet<string>());

            edges[edge.Item1].Add(edge.Item2);
            edges[edge.Item2].Add(edge.Item1);
        }

        Vector2[] positions = new Vector2[verticies.Length];

        for (int i = 0; i < positions.Length; i++)
            positions[i] = new Vector2(Random.Range(-.5F, .5F), Random.Range(-.5F, .5F));

        Vector2[] disp = new Vector2[verticies.Length];

        float area = AREA;

        float maxDist = (Mathf.Sqrt(area) / 10f);
        float k = Mathf.Sqrt(area) / (1f + positions.Length);


        for (int i = 0; i < ITERATIONS; i++)
        {
            for (int v = 0; v < verticies.Length; v++)
            {
                for (int u = 0; u < verticies.Length; u++)
                {
                    if (v == u)
                        continue;

                    Vector2 delta = positions[v] - positions[u];
                    float distance = delta.magnitude;

                    disp[v] -= delta / distance * (REPULSION_FACTOR * k * k / distance);

                    Vector2 dispPc = delta / distance * ComputeAttraction(edges, distance, k, snapshot.states[v], snapshot.states[u]);
                    disp[v] -= dispPc;
                    disp[u] += dispPc;
                }
            }

            for (int v = 0; v < verticies.Length; v++)
            {
                float dist = disp[v].magnitude;
                disp[v].x = disp[v].x - .01F * k * GRAVITY * positions[v].x;
                disp[v].y = disp[v].y - .01F * k * GRAVITY * positions[v].y;

                float distance = disp[v].magnitude;

                positions[v] += disp[v] / distance * Mathf.Min(distance, maxDist * SPEED);
                disp[v] = Vector2.zero;
            }
        }

        snapshot.positions = positions;
    }

    private float ComputeAttraction(Dictionary<string, HashSet<string>> edges, float distance, float k, string v, string u)
    {
        if (!edges.ContainsKey(v) && !edges.ContainsKey(u) && !edges[v].Contains(u) && !edges[u].Contains(v))
            return 0F;

        return distance * distance / k;
    }

    #endregion

    [MenuItem("Window/AI/AI Graph")]
    private static void ShowWindow()
        => GetWindow<AiGraphWindow>("AI Graph");
}