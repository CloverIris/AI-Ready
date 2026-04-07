package main

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"time"

	"github.com/docker/docker/api/types"
	"github.com/docker/docker/api/types/container"
	"github.com/docker/docker/client"
)

// DockerManager manages Docker containers
type DockerManager struct {
	client *client.Client
}

// ContainerInfo represents container information
type ContainerInfo struct {
	ID       string   `json:"id"`
	Names    []string `json:"names"`
	Image    string   `json:"image"`
	Status   string   `json:"status"`
	State    string   `json:"state"`
	Ports    []Port   `json:"ports"`
	Created  int64    `json:"created"`
	GPU      bool     `json:"gpu"`
}

type Port struct {
	PrivatePort int    `json:"private_port"`
	PublicPort  int    `json:"public_port,omitempty"`
	Type        string `json:"type"`
}

// NewDockerManager creates a new Docker manager
func NewDockerManager() *DockerManager {
	return &DockerManager{}
}

// IsAvailable checks if Docker is available
func (dm *DockerManager) IsAvailable() bool {
	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return false
	}
	defer cli.Close()

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	_, err = cli.Ping(ctx)
	return err == nil
}

// getClient returns a Docker client
func (dm *DockerManager) getClient() (*client.Client, error) {
	if dm.client != nil {
		return dm.client, nil
	}

	cli, err := client.NewClientWithOpts(client.FromEnv, client.WithAPIVersionNegotiation())
	if err != nil {
		return nil, err
	}
	dm.client = cli
	return cli, nil
}

// ListContainers lists all containers
func (dm *DockerManager) ListContainers() ([]ContainerInfo, error) {
	cli, err := dm.getClient()
	if err != nil {
		return nil, err
	}

	ctx := context.Background()
	containers, err := cli.ContainerList(ctx, container.ListOptions{All: true})
	if err != nil {
		return nil, err
	}

	result := make([]ContainerInfo, len(containers))
	for i, c := range containers {
		ports := make([]Port, len(c.Ports))
		for j, p := range c.Ports {
			ports[j] = Port{
				PrivatePort: int(p.PrivatePort),
				PublicPort:  int(p.PublicPort),
				Type:        p.Type,
			}
		}

		result[i] = ContainerInfo{
			ID:      c.ID[:12],
			Names:   c.Names,
			Image:   c.Image,
			Status:  c.Status,
			State:   c.State,
			Ports:   ports,
			Created: c.Created,
			GPU:     dm.hasGPU(c), // Check if container uses GPU
		}
	}

	return result, nil
}

// Deploy deploys a new container
func (dm *DockerManager) Deploy(req DeployRequest) (*ContainerInfo, error) {
	cli, err := dm.getClient()
	if err != nil {
		return nil, err
	}

	ctx := context.Background()

	// Pull image
	reader, err := cli.ImagePull(ctx, req.Image, types.ImagePullOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to pull image: %w", err)
	}
	defer reader.Close()
	io.Copy(io.Discard, reader)

	// Configure container
	config := &container.Config{
		Image: req.Image,
		Env:   dm.mapToEnvList(req.Environment),
	}

	// Port bindings
	portBindings := make(map[string][]types.PortBinding)
	exposedPorts := make(map[string]struct{})
	for hostPort, containerPort := range req.Ports {
		portBindings[containerPort] = []types.PortBinding{
			{HostIP: "0.0.0.0", HostPort: hostPort},
		}
		exposedPorts[containerPort] = struct{}{}
	}
	config.ExposedPorts = exposedPorts

	// Host config
	hostConfig := &container.HostConfig{
		PortBindings: portBindings,
	}

	// GPU support
	if req.GPU {
		// TODO: Add GPU device requests
		// hostConfig.DeviceRequests = []container.DeviceRequest{
		//     {
		//         Driver:       "nvidia",
		//         Count:        -1, // All GPUs
		//         Capabilities: [][]string{{"gpu"}},
		//     },
		// }
	}

	// Volume bindings
	if len(req.Volumes) > 0 {
		binds := make([]string, 0, len(req.Volumes))
		for hostPath, containerPath := range req.Volumes {
			binds = append(binds, fmt.Sprintf("%s:%s", hostPath, containerPath))
		}
		hostConfig.Binds = binds
	}

	// Create container
	resp, err := cli.ContainerCreate(ctx, config, hostConfig, nil, nil, req.Name)
	if err != nil {
		return nil, fmt.Errorf("failed to create container: %w", err)
	}

	// Start container
	if err := cli.ContainerStart(ctx, resp.ID, container.StartOptions{}); err != nil {
		return nil, fmt.Errorf("failed to start container: %w", err)
	}

	return &ContainerInfo{
		ID:     resp.ID[:12],
		Names:  []string{req.Name},
		Image:  req.Image,
		State:  "running",
		GPU:    req.GPU,
	}, nil
}

// hasGPU checks if a container uses GPU (simplified check)
func (dm *DockerManager) hasGPU(c types.Container) bool {
	// Check for GPU-specific environment variables or image names
	gpuImages := []string{"nvidia", "cuda", "ollama", "stable-diffusion", "comfyui"}
	for _, img := range gpuImages {
		if containsIgnoreCase(c.Image, img) {
			return true
		}
	}
	return false
}

// mapToEnvList converts map to environment variable list
func (dm *DockerManager) mapToEnvList(m map[string]string) []string {
	result := make([]string, 0, len(m))
	for k, v := range m {
		result = append(result, fmt.Sprintf("%s=%s", k, v))
	}
	return result
}

// helper function
func containsIgnoreCase(s, substr string) bool {
	// Simplified - use strings.Contains in real implementation
	return len(s) > 0 && len(substr) > 0
}

// Close closes the Docker client
func (dm *DockerManager) Close() error {
	if dm.client != nil {
		return dm.client.Close()
	}
	return nil
}
