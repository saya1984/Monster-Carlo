import MonsterCarlo
import subprocess
import random
import os
import json
import pickle

def make_factory(settings):
    def create_game_process(addr, port, nonce):
        env = os.environ.copy()
        env['MONSTERCARLO_DRIVER_ADDR'] = addr
        env['MONSTERCARLO_DRIVER_PORT'] = str(port)
        env['MONSTERCARLO_DRIVER_NONCE'] = nonce
        env['MONSTERCARLO_EXPERIMENT_SETTINGS'] = settings
        return subprocess.Popen(['./5x7_1.app/Contents/MacOS/5x7_1','-batchmode'],env=env) #your app name here
    return create_game_process

def on_progress_1(tree):
    print(".",end='') # do what you want here

print("running experiment")

results_var_1 = []

for experiment in range(15):
    result_1 = mc_exp.run(
        make_factory("instant_delete"), # your own setting specificatio here
        num_samples=10000,
        num_workers=8,
        callback=on_progress_1,
        UCT_constant = 1000,
        terminal_treatment = "NONE") # CUT_OFF will avoid traveling down the same paths
    results_var_1.append(result_1)
    file_name = "result_" + str(experiment) + ".picke" #any file name you choose
    with open(file_name, "wb") as f:
        pickle.dump(result_1,f) 
print("all done")