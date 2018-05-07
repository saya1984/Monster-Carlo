using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System;
using Random = System.Random;


public class MonsterCarloSupport : MonoBehaviour 
{
    protected TcpClient tcpClient = new TcpClient();
    protected StreamWriter writer = null;
    protected StreamReader reader = null;
    protected Random random = null;

    public bool IsDriverPresent 
    {
        get 
        {
            return Environment.GetEnvironmentVariable("MONSTERCARLO_DRIVER_NONCE") != null;
        }
        set { }
    }

    public string DesignVariant 
    {
        get 
        {
            return Environment.GetEnvironmentVariable("MONSTERCARLO_EXPERIMENT_SETTINGS");
        }
        set { }
    }

    public void Connect() 
    {
        string nonce = Environment.GetEnvironmentVariable("MONSTERCARLO_DRIVER_NONCE");
        string address = Environment.GetEnvironmentVariable("MONSTERCARLO_DRIVER_ADDR");
        int port = Int32.Parse(Environment.GetEnvironmentVariable("MONSTERCARLO_DRIVER_PORT"));
        Connect(address, port, nonce);
    }

    public void Connect(string address, int port, string nonce)
    {
        tcpClient.Connect(address, port);
        NetworkStream stream = tcpClient.GetStream();
        writer = new StreamWriter(stream);
        writer.AutoFlush = true;
        reader = new StreamReader(stream);
        writer.WriteLine(nonce);
        random = new Random(nonce.GetHashCode());
    }

    [Serializable]
    public class Step
    {
        public int a;//whichAction;
        public int c;//numChoices;
    }

    [Serializable]
    protected class Request
    {
        public Step[] prefix;
    }

    [Serializable]
    protected class Response
    {
        public Step[] path;
        public int score;
    }

    public class ExperimentFinishedException : Exception {}

    protected Queue<Step> prefix;
    protected Queue<Step> path;

    virtual public int Select(int limit, int[] weights = null, double temperature = 1) 
    {
        Assert.IsTrue(tcpClient.Connected);
        if (weights != null)
        {
            Assert.AreEqual(limit, weights.Length);
        }

        if (prefix == null)
        {
            string line = reader.ReadLine();
            if (line == null)
            {
                throw new ExperimentFinishedException();
            }
            Request request = JsonUtility.FromJson<Request>(line);
            prefix = new Queue<Step>(request.prefix);
            path = new Queue<Step>();
        }

        Step s;

        if (prefix.Count() > 0) 
        {
            s = prefix.Dequeue();
            Assert.AreEqual(limit, s.c);
        } 
        else 
        {
            s = new Step();
            s.c = limit;
            if (weights != null)
            {
                s.a = GetWeightedSelection(weights, temperature);
            }
            else
                s.a = random.Next(limit);
        }
        path.Enqueue(s);
        return s.a;
    }

    protected int GetWeightedSelection(int[] weights, double temperature)
    {
        int numWeights = weights.Count();
        double[] expWeights = new double[numWeights];
        double expSum = 0;
        for (int i = 0; i < numWeights; i++)
        {
            expWeights[i] = Math.Exp(weights[i]/temperature);
            expSum += expWeights[i];
        }
        double randomChoice = expSum * random.NextDouble();

        double total = 0;
        int selection = 0;
        for (selection = 0; selection < numWeights; selection++)
        {
            total += expWeights[selection];
            if (total >= randomChoice)
                break;
        }
        return selection;
    }


    virtual public void SupplyOutcome(int score)
    {
        Response response = new Response();
        response.score = score;
        response.path = path.ToArray();
        string line = JsonUtility.ToJson(response);
        writer.WriteLine(line);
        prefix = null;
        path = null;
    }
}