package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gorilla/mux"
)

// Version is the agent version
const Version = "1.0.0"

// Config holds the agent configuration
type Config struct {
	Port    string
	Version string
}

// Agent represents the AI-Ready agent
type Agent struct {
	config    *Config
	collector *SystemCollector
	docker    *DockerManager
	fileMgr   *FileManager
	startTime time.Time
}

// SystemInfo represents system information
type SystemInfo struct {
	Hostname   string    `json:"hostname"`
	Platform   string    `json:"platform"`
	CPU        CPUInfo   `json:"cpu"`
	Memory     MemInfo   `json:"memory"`
	GPUs       []GPUInfo `json:"gpus"`
	Disk       DiskInfo  `json:"disk"`
	AgentVer   string    `json:"agent_version"`
	Uptime     string    `json:"uptime"`
	Docker     bool      `json:"docker_available"`
}

type CPUInfo struct {
	Model        string  `json:"model"`
	Cores        int     `json:"cores"`
	Threads      int     `json:"threads"`
	UsagePercent float64 `json:"usage_percent"`
}

type MemInfo struct {
	Total       uint64  `json:"total"`
	Used        uint64  `json:"used"`
	Free        uint64  `json:"free"`
	UsedPercent float64 `json:"used_percent"`
}

type GPUInfo struct {
	Model       string `json:"model"`
	VRAMTotal   uint64 `json:"vram_total"`
	VRAMUsed    uint64 `json:"vram_used"`
	Temperature int    `json:"temperature"`
}

type DiskInfo struct {
	Total       uint64  `json:"total"`
	Used        uint64  `json:"used"`
	Free        uint64  `json:"free"`
	UsedPercent float64 `json:"used_percent"`
}

// NewAgent creates a new agent instance
func NewAgent(config *Config) *Agent {
	return &Agent{
		config:    config,
		collector: NewSystemCollector(),
		docker:    NewDockerManager(),
		fileMgr:   NewFileManager(),
		startTime: time.Now(),
	}
}

// handleSystemInfo returns system information
func (a *Agent) handleSystemInfo(w http.ResponseWriter, r *http.Request) {
	info, err := a.collector.Collect()
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	info.AgentVer = Version
	info.Uptime = time.Since(a.startTime).String()
	info.Docker = a.docker.IsAvailable()

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(info)
}

// handleHealth returns health status
func (a *Agent) handleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]string{
		"status":  "healthy",
		"version": Version,
	})
}

// handleContainers handles container operations
func (a *Agent) handleContainers(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		containers, err := a.docker.ListContainers()
		if err != nil {
			http.Error(w, err.Error(), http.StatusInternalServerError)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(containers)

	case http.MethodPost:
		// Deploy new container
		var req DeployRequest
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, err.Error(), http.StatusBadRequest)
			return
		}
		result, err := a.docker.Deploy(req)
		if err != nil {
			http.Error(w, err.Error(), http.StatusInternalServerError)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(result)

	default:
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
	}
}

// handleFiles handles file operations
func (a *Agent) handleFiles(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	path := vars["path"]

	switch r.Method {
	case http.MethodGet:
		// List directory or get file info
		info, err := a.fileMgr.List(path)
		if err != nil {
			http.Error(w, err.Error(), http.StatusInternalServerError)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(info)

	default:
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
	}
}

// DeployRequest represents a container deployment request
type DeployRequest struct {
	Name        string            `json:"name"`
	Image       string            `json:"image"`
	Ports       map[string]string `json:"ports"`
	Volumes     map[string]string `json:"volumes"`
	Environment map[string]string `json:"environment"`
	GPU         bool              `json:"gpu"`
}

func main() {
	config := &Config{
		Port:    getEnv("AIREADY_PORT", "18080"),
		Version: Version,
	}

	agent := NewAgent(config)

	router := mux.NewRouter()
	router.HandleFunc("/api/v1/health", agent.handleHealth).Methods("GET")
	router.HandleFunc("/api/v1/system/info", agent.handleSystemInfo).Methods("GET")
	router.HandleFunc("/api/v1/containers", agent.handleContainers).Methods("GET", "POST")
	router.HandleFunc("/api/v1/files/{path:.*}", agent.handleFiles).Methods("GET", "PUT", "DELETE")

	server := &http.Server{
		Addr:    "127.0.0.1:" + config.Port,
		Handler: router,
	}

	// Graceful shutdown
	go func() {
		sigChan := make(chan os.Signal, 1)
		signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)
		<-sigChan

		log.Println("Shutting down agent...")
		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		server.Shutdown(ctx)
	}()

	log.Printf("AI-Ready Agent v%s starting on %s", Version, server.Addr)
	if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("Failed to start server: %v", err)
	}
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}
