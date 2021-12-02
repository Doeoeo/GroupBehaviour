using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneticAlgorithmDemo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Demo start --> this is a demo of the genetic algorithm, it evolves an array of floats, so that their sum is the largest possible.");
        
        //                                                      pop. size,  mut. rate,  chromosome gene size,   max generations    score that ends algorithm if exceeded 
        GeneticAlgorithm geneticAlgorithm = new GeneticAlgorithm(100,       0.01f,      4,                      100,               38.0f);
        Debug.Log("Genetic algorithm created");
        Chromosome best = geneticAlgorithm.EvolvePredator();
        Debug.Log("Finished --> Best chromosome: " + best.ToString());

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
